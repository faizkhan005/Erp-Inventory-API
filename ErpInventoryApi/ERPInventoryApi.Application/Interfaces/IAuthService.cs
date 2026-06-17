using ERPInventoryApi.Application.DTOs.Auth;

namespace ERPInventoryApi.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);

    Task<AuthResponse> LoginAsync(LoginRequest request);

    Task<RefreshResponse> RefreshTokenAsync(string refreshToken); 

    Task RevokeTokenAsync(string refreshToken, string reason = "Logout"); 
}
