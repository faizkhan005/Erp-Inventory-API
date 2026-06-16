namespace ERPInventoryApi.Application.DTOs.Auth;

public record RegisterRequest(string Username, string Password, string Role = "User");
