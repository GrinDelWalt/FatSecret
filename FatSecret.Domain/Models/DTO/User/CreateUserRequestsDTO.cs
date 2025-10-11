using System.ComponentModel.DataAnnotations;

namespace FatSecret.Domain.Models.DTO.User;

/// <summary>
/// DTO для создания нового пользователя с расширенными данными
/// </summary>
public class CreateUserRequestsDTO
{
    [Required(ErrorMessage = "Имя пользователя обязательно")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Имя пользователя должно содержать от 3 до 50 символов")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress(ErrorMessage = "Некорректный формат email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пароль обязателен")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен содержать от 6 до 100 символов")]
    public string Password { get; set; } = string.Empty;

    [Range(1, 120, ErrorMessage = "Возраст должен быть от 1 до 120 лет")]
    public int Age { get; set; }

    [Range(1, 1000, ErrorMessage = "Вес должен быть от 1 до 1000 кг")]
    public decimal Weight { get; set; }

    [Range(1, 300, ErrorMessage = "Рост должен быть от 1 до 300 см")]
    public int Height { get; set; }

    [StringLength(20)]
    public string? Gender { get; set; } // "male", "female", etc.

    [StringLength(20)]
    public string Goal { get; set; } = "maintain"; // "lose", "maintain", "gain"

    [StringLength(20)]
    public string ActivityLevel { get; set; } = "moderate"; // "low", "moderate", "high", "very-high"

    // Рассчитываются автоматически сервером
    public int BasalMetabolicRate { get; set; }
    public int DailyCalorieTarget { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}


/// <summary>
/// DTO полного профиля пользователя
/// </summary>
public record UserProfileDTO(
    int UserId,
    string Username,
    string Email,
    int Age,
    decimal Weight,
    int Height,
    string? Gender,
    string Goal,
    string ActivityLevel,
    int BasalMetabolicRate,
    int DailyCalorieTarget,
    string? Notes,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    UserStatsDTO Stats
);

/// <summary>
/// DTO для статистики пользователя
/// </summary>
public record UserStatsDTO(
    int TotalDays,
    int DaysWithEntries,
    decimal AverageCaloriesPerDay,
    DateTime? LastEntryDate
);

/// <summary>
/// DTO для обновления профиля пользователя
/// </summary>
public class UpdateUserProfileDTO
{
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Имя пользователя должно содержать от 3 до 50 символов")]
    public string? Username { get; set; }

    [EmailAddress(ErrorMessage = "Некорректный формат email")]
    public string? Email { get; set; }

    [Range(1, 120, ErrorMessage = "Возраст должен быть от 1 до 120 лет")]
    public int? Age { get; set; }

    [Range(1, 1000, ErrorMessage = "Вес должен быть от 1 до 1000 кг")]
    public decimal? Weight { get; set; }

    [Range(1, 300, ErrorMessage = "Рост должен быть от 1 до 300 см")]
    public int? Height { get; set; }

    [StringLength(20)]
    public string? Gender { get; set; }

    [StringLength(20)]
    public string? Goal { get; set; }

    [StringLength(20)]
    public string? ActivityLevel { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    // Рассчитываются автоматически сервером
    public int BasalMetabolicRate { get; set; }
    public int DailyCalorieTarget { get; set; }
}

/// <summary>
/// DTO для смены пароля
/// </summary>
public class ChangePasswordDTO
{
    [Required(ErrorMessage = "Текущий пароль обязателен")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Новый пароль обязателен")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен содержать от 6 до 100 символов")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Подтверждение пароля обязательно")]
    [Compare(nameof(NewPassword), ErrorMessage = "Пароли не совпадают")]
    public string ConfirmPassword { get; set; } = string.Empty;
}


/// <summary>
/// DTO для сброса пароля
/// </summary>
public class ResetPasswordRequestDTO
{
    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress(ErrorMessage = "Некорректный формат email")]
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// DTO для подтверждения сброса пароля
/// </summary>
public class ResetPasswordConfirmDTO
{
    [Required(ErrorMessage = "Токен обязателен")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress(ErrorMessage = "Некорректный формат email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Новый пароль обязателен")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен содержать от 6 до 100 символов")]
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
/// DTO для валидации токена
/// </summary>
public class ValidateTokenDTO
{
    [Required(ErrorMessage = "Токен обязателен")]
    public string Token { get; set; } = string.Empty;
}

/// <summary>
/// DTO ответа при валидации токена
/// </summary>
public record TokenValidationResponseDTO(
    bool IsValid,
    string? Username,
    DateTime? ExpiresAt,
    string? Message
);