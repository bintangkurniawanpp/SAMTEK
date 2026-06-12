namespace SAMTEK.Application.DTOs;

public class LoginRequestDto
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}

public record LoginResponseDto(string Token, string Username, string DisplayName, string AppaRole, DateTime ExpiresAt);
