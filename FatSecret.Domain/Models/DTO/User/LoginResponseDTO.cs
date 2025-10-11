using System.ComponentModel.DataAnnotations;

namespace FatSecret.Domain.Models.DTO;

public class LoginRequestDTO
{
    [Required(ErrorMessage = "Логин обязателен")]
    public string Login { get; set; } = string.Empty; // Email или Username

    [Required(ErrorMessage = "Пароль обязателен")]
    public string Password { get; set; } = string.Empty;
}
public record LoginResponseDTO(
    string Token,
    string Username,
    string Email,
    DateTime ExpiresAt
);