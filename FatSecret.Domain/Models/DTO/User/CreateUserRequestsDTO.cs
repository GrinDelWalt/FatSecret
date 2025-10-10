namespace FatSecret.Domain.Models.DTO;

public record CreateUserRequestsDTO(string Email, string Password, string LastName, string FirstName, string Login);
