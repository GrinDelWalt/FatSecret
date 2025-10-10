namespace FatSecret.Domain.Models.DTO;

public record LoginRequestDTO(string Login, string Password);

public record LoginResponseDTO(string Token, string Login, string Email);