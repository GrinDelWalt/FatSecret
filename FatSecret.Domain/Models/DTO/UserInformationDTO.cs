namespace FatSecret.Domain.Models.DTO;

public record UserInformationDTO(
    string Email,
    string Username,
    string? DisplayName = null
);