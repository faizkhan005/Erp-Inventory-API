using ERPInventoryApi.Application.DTOs.Auth;
using ERPInventoryApi.Application.Interfaces;
using ERPInventoryApi.Domain.Entities;
using ERPInventoryApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ERPInventoryApi.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<AuthService> _logger;

    private static readonly TimeSpan RefreshTokenTtl = TimeSpan.FromDays(7);

    public AuthService(
        AppDbContext db,
        IConfiguration config,
        IConnectionMultiplexer redis,
        ILogger<AuthService> logger)
    {
        _db = db;
        _config = config;
        _redis = redis;
        _logger = logger;
    }

    // Register

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var exists = await _db.Users.AnyAsync(u => u.Username == request.Username);
        if (exists)
            throw new InvalidOperationException($"Username '{request.Username}' is already taken.");

        var user = new User
        {
            Username = request.Username,
            PasswordHash = HashPassword(request.Password),
            Role = request.Role
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var (accessToken, accessExpiry) = GenerateAccessToken(user);
        var refreshToken = await CreateAndStoreRefreshTokenAsync(user.Id);

        return new AuthResponse(accessToken, user.Username, user.Role, accessExpiry, refreshToken.Token);
    }

    // Login

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username)
            ?? throw new UnauthorizedAccessException("Invalid username or password.");

        if (!VerifyPassword(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid username or password.");

        var (accessToken, accessExpiry) = GenerateAccessToken(user);
        var refreshToken = await CreateAndStoreRefreshTokenAsync(user.Id);

        return new AuthResponse(accessToken, user.Username, user.Role, accessExpiry, refreshToken.Token);
    }

    // Refresh 

    public async Task<RefreshResponse> RefreshTokenAsync(string token)
    {
        // 1. Check Redis first (fast path)
        var redisDb = _redis.GetDatabase();
        var redisKey = $"refresh:{token}";
        var cachedUserId = await redisDb.StringGetAsync(redisKey);

        RefreshToken? refreshToken;

        if (cachedUserId.HasValue)
        {
            // Redis hit — still verify in DB for revocation status
            refreshToken = await _db.RefreshTokens
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Token == token);
        }
        else
        {
            // Redis miss — go to DB
            refreshToken = await _db.RefreshTokens
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Token == token);
        }

        if (refreshToken is null)
            throw new UnauthorizedAccessException("Invalid refresh token.");

        // 2. Detect token reuse — if revoked, someone is replaying an old token
        if (refreshToken.IsRevoked)
        {
            _logger.LogWarning(
                "Refresh token reuse detected for user {UserId}. Token: {Token}",
                refreshToken.UserId, token);

            // Revoke all tokens for this user — potential compromise
            await RevokeAllUserTokensAsync(refreshToken.UserId, "Reuse detected");
            throw new UnauthorizedAccessException("Refresh token reuse detected. All sessions revoked.");
        }

        if (refreshToken.IsExpired)
            throw new UnauthorizedAccessException("Refresh token has expired. Please log in again.");

        // 3. Rotate — revoke old token, issue new one
        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevokedReason = "Rotated";

        var newRefreshToken = await CreateAndStoreRefreshTokenAsync(refreshToken.UserId);
        refreshToken.ReplacedByToken = newRefreshToken.Token;

        await _db.SaveChangesAsync();

        // Remove old token from Redis
        await redisDb.KeyDeleteAsync(redisKey);

        var (accessToken, accessExpiry) = GenerateAccessToken(refreshToken.User);
        return new RefreshResponse(accessToken, refreshToken.User.Username, refreshToken.User.Role, accessExpiry);
    }

    // Revoke (Logout)

    public async Task RevokeTokenAsync(string token, string reason = "Logout")
    {
        var refreshToken = await _db.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == token);

        if (refreshToken is null || !refreshToken.IsActive)
            return; // already revoked or doesn't exist — silently succeed

        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevokedReason = reason;
        await _db.SaveChangesAsync();

        // Remove from Redis
        var redisDb = _redis.GetDatabase();
        await redisDb.KeyDeleteAsync($"refresh:{token}");
    }

    // Private helpers 

    private async Task<RefreshToken> CreateAndStoreRefreshTokenAsync(Guid userId)
    {
        var token = new RefreshToken
        {
            Token = GenerateSecureToken(),
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.Add(RefreshTokenTtl)
        };

        _db.RefreshTokens.Add(token);
        await _db.SaveChangesAsync();

        // Store in Redis with same TTL for fast lookup
        var redisDb = _redis.GetDatabase();
        await redisDb.StringSetAsync(
            $"refresh:{token.Token}",
            userId.ToString(),
            RefreshTokenTtl);

        return token;
    }

    private async Task RevokeAllUserTokensAsync(Guid userId, string reason)
    {
        var activeTokens = await _db.RefreshTokens
            .Where(r => r.UserId == userId && r.RevokedAt == null)
            .ToListAsync();

        var redisDb = _redis.GetDatabase();

        foreach (var t in activeTokens)
        {
            t.RevokedAt = DateTime.UtcNow;
            t.RevokedReason = reason;
            await redisDb.KeyDeleteAsync($"refresh:{t.Token}");
        }

        await _db.SaveChangesAsync();
    }

    private (string token, DateTime expiry) GenerateAccessToken(User user)
    {
        var jwtSettings = _config.GetSection("JwtSettings");
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry = DateTime.UtcNow.AddMinutes(
            double.Parse(jwtSettings["ExpiryMinutes"]!));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,        user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(ClaimTypes.Role,                    user.Role),
            new Claim(JwtRegisteredClaimNames.Jti,        Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: expiry,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiry);
    }

    private static string GenerateSecureToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", ""); // URL-safe Base64

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var saltedPassword = salt.Concat(Encoding.UTF8.GetBytes(password)).ToArray();
        var hash = SHA256.HashData(saltedPassword);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split(':');
        if (parts.Length != 2) return false;
        var salt = Convert.FromBase64String(parts[0]);
        var expectedHash = Convert.FromBase64String(parts[1]);
        var actualHash = SHA256.HashData(
            salt.Concat(Encoding.UTF8.GetBytes(password)).ToArray());
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
