using ERPInventoryApi.Application.DTOs.Auth;
using ERPInventoryApi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPInventoryApi.API.Controllers;

[ApiController]
[Route("api/[controller]")]  
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    private const string RefreshTokenCookieName = "refreshToken";

    private static readonly CookieOptions RefreshCookieOptions = new()
    {
        HttpOnly = true,           // JS cannot access — XSS protection
        Secure = true,           // HTTPS only — set false for local HTTP dev
        SameSite = SameSiteMode.Strict,
        Expires = DateTimeOffset.UtcNow.AddDays(7)
    };

    public AuthController(IAuthService authService) => _authService = authService;

    /// <summary> Register a new user and receive a JWT.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        SetRefreshTokenCookie(result.RefreshToken);

        return Ok(result with { RefreshToken = string.Empty });
    }

    /// <summary> Login and receive a JWT.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        SetRefreshTokenCookie(result.RefreshToken);
        return Ok(result with { RefreshToken = string.Empty });
    }

    /// <summary> Request a new access token using a refresh token.
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Refresh()
    {
        // Read the refresh token from the HttpOnly cookie
        var token = Request.Cookies[RefreshTokenCookieName];
        if (string.IsNullOrWhiteSpace(token))
            return Unauthorized("Refresh token cookie is missing.");

        var result = await _authService.RefreshTokenAsync(token);

        // Rotate — new refresh token set in cookie automatically
        // The new refresh token comes back via a re-login on the next refresh
        // For now the same cookie is valid until rotated
        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        var token = Request.Cookies[RefreshTokenCookieName];
        if (!string.IsNullOrWhiteSpace(token))
            await _authService.RevokeTokenAsync(token);

        // Clear the cookie
        Response.Cookies.Delete(RefreshTokenCookieName);
        return NoContent();
    }

    //Helper

    private void SetRefreshTokenCookie(string token)
    {
        Response.Cookies.Append(RefreshTokenCookieName, token, RefreshCookieOptions);
    }

}
