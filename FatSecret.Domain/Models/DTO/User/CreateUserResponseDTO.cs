namespace FatSecret.Domain.Models.DTO;

public record CreateUserResponseDTO(string Email, string Password, string LastName, string FirstName, string Login);
