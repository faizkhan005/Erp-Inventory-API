namespace ERPInventoryApi.Application.DTOs.Auth;

public record AuthResponse(string Token, string Username, string Role, DateTime ExpiresAt, string RefreshToken);
