namespace MyStudents.Application.Auth.Dto;

public record LoginRequest(string Username, string Password);

public record RegisterRequest(string Username, string Email, string Password, string FullName);

public record RefreshTokenRequest(string AccessToken, string RefreshToken);

public record AuthResponse(string Token, string RefreshToken, string Username, string Role, Guid UserId);

public record UserDto(Guid Id, string Username, string Email, string FullName, string Role);
