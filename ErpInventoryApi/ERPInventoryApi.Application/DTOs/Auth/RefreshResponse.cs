namespace ERPInventoryApi.Application.DTOs.Auth;

public record RefreshResponse(
    string AccessToken,
    string Username,
    string Role,
    DateTime ExpiresAt
);