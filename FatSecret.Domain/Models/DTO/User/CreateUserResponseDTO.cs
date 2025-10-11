namespace FatSecret.Domain.Models.DTO.User;

/// <summary>
/// DTO ответа при создании пользователя
/// </summary>
public record CreateUserResponseDTO(
    int UserId,
    string Username,
    string Email,
    int BasalMetabolicRate,
    int DailyCalorieTarget,
    DateTime CreatedAt
);
