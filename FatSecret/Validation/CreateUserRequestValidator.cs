using FluentValidation;
using FatSecret.Domain.Models.DTO;
using System.Text.RegularExpressions;
using FatSecret.Domain.Models.DTO.User;

namespace FatSecret.Validation;

/// <summary>
/// Валидатор для регистрации пользователя
/// </summary>
public class CreateUserRequestValidator : AbstractValidator<CreateUserRequestsDTO>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Имя пользователя обязательно")
            .Length(3, 50).WithMessage("Имя пользователя должно содержать от 3 до 50 символов")
            .Matches(@"^[a-zA-Z0-9_а-яА-Я]+$").WithMessage("Имя пользователя может содержать только буквы, цифры и подчеркивания");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email обязателен")
            .EmailAddress().WithMessage("Некорректный формат email")
            .MaximumLength(255).WithMessage("Email не может быть длиннее 255 символов");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Пароль обязателен")
            .MinimumLength(6).WithMessage("Пароль должен содержать минимум 6 символов")
            .MaximumLength(100).WithMessage("Пароль не может быть длиннее 100 символов")
            .Must(BeValidPassword).WithMessage("Пароль должен содержать хотя бы одну букву и один символ");

        When(x => x.Age > 0, () => {
            RuleFor(x => x.Age)
                .GreaterThan(0).WithMessage("Возраст должен быть больше 0")
                .LessThanOrEqualTo(120).WithMessage("Возраст не может быть больше 120 лет");
        });

        When(x => x.Weight > 0, () => {
            RuleFor(x => x.Weight)
                .GreaterThan(0).WithMessage("Вес должен быть больше 0")
                .LessThanOrEqualTo(1000).WithMessage("Вес не может быть больше 1000 кг");
        });

        When(x => x.Height > 0, () => {
            RuleFor(x => x.Height)
                .GreaterThan(0).WithMessage("Рост должен быть больше 0")
                .LessThanOrEqualTo(300).WithMessage("Рост не может быть больше 300 см");
        });

        When(x => !string.IsNullOrEmpty(x.Gender), () => {
            RuleFor(x => x.Gender)
                .Must(BeValidGender).WithMessage("Недопустимое значение пола. Допустимые значения: male, female, other");
        });

        RuleFor(x => x.Goal)
            .NotEmpty().WithMessage("Цель обязательна")
            .Must(BeValidGoal).WithMessage("Недопустимое значение цели. Допустимые значения: lose, maintain, gain");

        RuleFor(x => x.ActivityLevel)
            .NotEmpty().WithMessage("Уровень активности обязателен")
            .Must(BeValidActivityLevel).WithMessage("Недопустимый уровень активности. Допустимые значения: low, moderate, high, very-high");

        When(x => !string.IsNullOrEmpty(x.Notes), () => {
            RuleFor(x => x.Notes)
                .MaximumLength(500).WithMessage("Заметки не могут быть длиннее 500 символов");
        });
    }

    private static bool BeValidPassword(string password)
    {
        if (string.IsNullOrEmpty(password)) return false;
        
        // Должен содержать хотя бы одну букву
        var hasLetter = Regex.IsMatch(password, @"[a-zA-Zа-яА-Я]");
        
        return hasLetter;
    }

    private static bool BeValidGender(string? gender)
    {
        if (string.IsNullOrEmpty(gender)) return true;
        
        var validGenders = new[] { "male", "female", "other", "м", "ж", "мужской", "женский", "другой" };
        return validGenders.Contains(gender.ToLowerInvariant());
    }

    private static bool BeValidGoal(string? goal)
    {
        if (string.IsNullOrEmpty(goal)) return false;
        
        var validGoals = new[] { "lose", "maintain", "gain", "похудение", "поддержание", "набор" };
        return validGoals.Contains(goal.ToLowerInvariant());
    }

    private static bool BeValidActivityLevel(string? activityLevel)
    {
        if (string.IsNullOrEmpty(activityLevel)) return false;
        
        var validLevels = new[] { "low", "moderate", "high", "very-high", "низкая", "умеренная", "высокая", "очень-высокая" };
        return validLevels.Contains(activityLevel.ToLowerInvariant());
    }
}

/// <summary>
/// Валидатор для входа в систему
/// </summary>
public class LoginRequestValidator : AbstractValidator<LoginRequestDTO>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Login)
            .NotEmpty().WithMessage("Логин обязателен")
            .MaximumLength(255).WithMessage("Логин не может быть длиннее 255 символов");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Пароль обязателен")
            .MaximumLength(100).WithMessage("Пароль не может быть длиннее 100 символов");
    }
}

/// <summary>
/// Валидатор для обновления профиля
/// </summary>
public class UpdateUserProfileValidator : AbstractValidator<UpdateUserProfileDTO>
{
    public UpdateUserProfileValidator()
    {
        When(x => !string.IsNullOrEmpty(x.Username), () => {
            RuleFor(x => x.Username)
                .Length(3, 50).WithMessage("Имя пользователя должно содержать от 3 до 50 символов")
                .Matches(@"^[a-zA-Z0-9_а-яА-Я]+$").WithMessage("Имя пользователя может содержать только буквы, цифры и подчеркивания");
        });

        When(x => !string.IsNullOrEmpty(x.Email), () => {
            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Некорректный формат email")
                .MaximumLength(255).WithMessage("Email не может быть длиннее 255 символов");
        });

        When(x => x.Age.HasValue, () => {
            RuleFor(x => x.Age)
                .GreaterThan(0).WithMessage("Возраст должен быть больше 0")
                .LessThanOrEqualTo(120).WithMessage("Возраст не может быть больше 120 лет");
        });

        When(x => x.Weight.HasValue, () => {
            RuleFor(x => x.Weight)
                .GreaterThan(0).WithMessage("Вес должен быть больше 0")
                .LessThanOrEqualTo(1000).WithMessage("Вес не может быть больше 1000 кг");
        });

        When(x => x.Height.HasValue, () => {
            RuleFor(x => x.Height)
                .GreaterThan(0).WithMessage("Рост должен быть больше 0")
                .LessThanOrEqualTo(300).WithMessage("Рост не может быть больше 300 см");
        });

        When(x => !string.IsNullOrEmpty(x.Gender), () => {
            RuleFor(x => x.Gender)
                .Must(BeValidGender).WithMessage("Недопустимое значение пола. Допустимые значения: male, female, other");
        });

        When(x => !string.IsNullOrEmpty(x.Goal), () => {
            RuleFor(x => x.Goal)
                .Must(BeValidGoal).WithMessage("Недопустимое значение цели. Допустимые значения: lose, maintain, gain");
        });

        When(x => !string.IsNullOrEmpty(x.ActivityLevel), () => {
            RuleFor(x => x.ActivityLevel)
                .Must(BeValidActivityLevel).WithMessage("Недопустимый уровень активности. Допустимые значения: low, moderate, high, very-high");
        });

        When(x => x.Notes != null, () => {
            RuleFor(x => x.Notes)
                .MaximumLength(500).WithMessage("Заметки не могут быть длиннее 500 символов");
        });
    }

    private static bool BeValidGender(string? gender)
    {
        if (string.IsNullOrEmpty(gender)) return true;
        
        var validGenders = new[] { "male", "female", "other", "м", "ж", "мужской", "женский", "другой" };
        return validGenders.Contains(gender.ToLowerInvariant());
    }

    private static bool BeValidGoal(string? goal)
    {
        if (string.IsNullOrEmpty(goal)) return true;
        
        var validGoals = new[] { "lose", "maintain", "gain", "похудение", "поддержание", "набор" };
        return validGoals.Contains(goal.ToLowerInvariant());
    }

    private static bool BeValidActivityLevel(string? activityLevel)
    {
        if (string.IsNullOrEmpty(activityLevel)) return true;
        
        var validLevels = new[] { "low", "moderate", "high", "very-high", "низкая", "умеренная", "высокая", "очень-высокая" };
        return validLevels.Contains(activityLevel.ToLowerInvariant());
    }
}

/// <summary>
/// Валидатор для смены пароля
/// </summary>
public class ChangePasswordValidator : AbstractValidator<ChangePasswordDTO>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Текущий пароль обязателен")
            .MaximumLength(100).WithMessage("Пароль не может быть длиннее 100 символов");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Новый пароль обязателен")
            .MinimumLength(6).WithMessage("Пароль должен содержать минимум 6 символов")
            .MaximumLength(100).WithMessage("Пароль не может быть длиннее 100 символов")
            .Must(BeValidPassword).WithMessage("Пароль должен содержать хотя бы одну букву")
            .NotEqual(x => x.CurrentPassword).WithMessage("Новый пароль должен отличаться от текущего");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Подтверждение пароля обязательно")
            .Equal(x => x.NewPassword).WithMessage("Пароли не совпадают");
    }

    private static bool BeValidPassword(string password)
    {
        if (string.IsNullOrEmpty(password)) return false;
        
        // Должен содержать хотя бы одну букву
        var hasLetter = Regex.IsMatch(password, @"[a-zA-Zа-яА-Я]");
        
        return hasLetter;
    }
}

/// <summary>
/// Валидатор для сброса пароля
/// </summary>
public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequestDTO>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email обязателен")
            .EmailAddress().WithMessage("Некорректный формат email")
            .MaximumLength(255).WithMessage("Email не может быть длиннее 255 символов");
    }
}

/// <summary>
/// Валидатор для подтверждения сброса пароля
/// </summary>
public class ResetPasswordConfirmValidator : AbstractValidator<ResetPasswordConfirmDTO>
{
    public ResetPasswordConfirmValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Токен обязателен")
            .MinimumLength(10).WithMessage("Недопустимый формат токена");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email обязателен")
            .EmailAddress().WithMessage("Некорректный формат email")
            .MaximumLength(255).WithMessage("Email не может быть длиннее 255 символов");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Новый пароль обязателен")
            .MinimumLength(6).WithMessage("Пароль должен содержать минимум 6 символов")
            .MaximumLength(100).WithMessage("Пароль не может быть длиннее 100 символов")
            .Must(BeValidPassword).WithMessage("Пароль должен содержать хотя бы одну букву");
    }

    private static bool BeValidPassword(string password)
    {
        if (string.IsNullOrEmpty(password)) return false;
        
        // Должен содержать хотя бы одну букву
        var hasLetter = Regex.IsMatch(password, @"[a-zA-Zа-яА-Я]");
        
        return hasLetter;
    }
}

/// <summary>
/// Валидатор для проверки токена
/// </summary>
public class ValidateTokenValidator : AbstractValidator<ValidateTokenDTO>
{
    public ValidateTokenValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Токен обязателен")
            .MinimumLength(10).WithMessage("Недопустимый формат токена");
    }
}

/// <summary>
/// Валидатор для записи веса
/// </summary>
public class WeightRecordValidator : AbstractValidator<WeightRecordDTO>
{
    public WeightRecordValidator()
    {
        RuleFor(x => x.Weight)
            .GreaterThan(0).WithMessage("Вес должен быть больше 0")
            .LessThanOrEqualTo(1000).WithMessage("Вес не может быть больше 1000 кг");

        RuleFor(x => x.RecordedDate)
            .NotEmpty().WithMessage("Дата записи обязательна")
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today)).WithMessage("Дата записи не может быть в будущем");

        When(x => !string.IsNullOrEmpty(x.Notes), () => {
            RuleFor(x => x.Notes)
                .MaximumLength(500).WithMessage("Заметки не могут быть длиннее 500 символов");
        });
    }
}

/// <summary>
/// Валидатор для записи еды
/// </summary>
public class FoodEntryValidator : AbstractValidator<FoodEntryDTO>
{
    public FoodEntryValidator()
    {
        RuleFor(x => x.FoodName)
            .NotEmpty().WithMessage("Название еды обязательно")
            .MaximumLength(200).WithMessage("Название еды не может быть длиннее 200 символов");

        RuleFor(x => x.Calories)
            .GreaterThanOrEqualTo(0).WithMessage("Калории не могут быть отрицательными")
            .LessThanOrEqualTo(10000).WithMessage("Калории не могут превышать 10000");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Количество должно быть больше 0")
            .LessThanOrEqualTo(10000).WithMessage("Количество не может превышать 10000");

        RuleFor(x => x.Unit)
            .NotEmpty().WithMessage("Единица измерения обязательна")
            .MaximumLength(50).WithMessage("Единица измерения не может быть длиннее 50 символов");

        RuleFor(x => x.EntryDate)
            .NotEmpty().WithMessage("Дата записи обязательна")
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today)).WithMessage("Дата записи не может быть в будущем");

        RuleFor(x => x.MealType)
            .NotEmpty().WithMessage("Тип приема пищи обязателен")
            .Must(BeValidMealType).WithMessage("Недопустимый тип приема пищи. Допустимые значения: breakfast, lunch, dinner, snack, other");
    }

    private static bool BeValidMealType(string mealType)
    {
        if (string.IsNullOrEmpty(mealType)) return false;
        
        var validMealTypes = new[] { "breakfast", "lunch", "dinner", "snack", "other", "завтрак", "обед", "ужин", "перекус", "другое" };
        return validMealTypes.Contains(mealType.ToLowerInvariant());
    }
}

/// <summary>
/// Валидатор для настроек пользователя
/// </summary>
public class UserPreferencesValidator : AbstractValidator<UserPreferencesDTO>
{
    public UserPreferencesValidator()
    {
        RuleFor(x => x.Language)
            .MaximumLength(10).WithMessage("Код языка не может быть длиннее 10 символов")
            .Must(BeValidLanguage).WithMessage("Недопустимый код языка. Допустимые значения: ru, en, es, fr, de")
            .When(x => !string.IsNullOrEmpty(x.Language));

        RuleFor(x => x.Timezone)
            .MaximumLength(50).WithMessage("Часовой пояс не может быть длиннее 50 символов")
            .When(x => !string.IsNullOrEmpty(x.Timezone));

        RuleFor(x => x.DateFormat)
            .MaximumLength(20).WithMessage("Формат даты не может быть длиннее 20 символов")
            .Must(BeValidDateFormat).WithMessage("Недопустимый формат даты. Допустимые значения: dd.MM.yyyy, MM/dd/yyyy, yyyy-MM-dd")
            .When(x => !string.IsNullOrEmpty(x.DateFormat));

        RuleFor(x => x.WeightUnit)
            .MaximumLength(10).WithMessage("Единица веса не может быть длиннее 10 символов")
            .Must(BeValidWeightUnit).WithMessage("Недопустимая единица веса. Допустимые значения: kg, lbs, st")
            .When(x => !string.IsNullOrEmpty(x.WeightUnit));

        RuleFor(x => x.HeightUnit)
            .MaximumLength(10).WithMessage("Единица роста не может быть длиннее 10 символов")
            .Must(BeValidHeightUnit).WithMessage("Недопустимая единица роста. Допустимые значения: cm, in, ft")
            .When(x => !string.IsNullOrEmpty(x.HeightUnit));
    }

    private static bool BeValidLanguage(string? language)
    {
        if (string.IsNullOrEmpty(language)) return true;
        
        var validLanguages = new[] { "ru", "en", "es", "fr", "de", "it", "pt", "zh", "ja", "ko" };
        return validLanguages.Contains(language.ToLowerInvariant());
    }

    private static bool BeValidDateFormat(string? dateFormat)
    {
        if (string.IsNullOrEmpty(dateFormat)) return true;
        
        var validFormats = new[] { "dd.MM.yyyy", "MM/dd/yyyy", "yyyy-MM-dd", "dd/MM/yyyy", "MM-dd-yyyy" };
        return validFormats.Contains(dateFormat);
    }

    private static bool BeValidWeightUnit(string? weightUnit)
    {
        if (string.IsNullOrEmpty(weightUnit)) return true;
        
        var validUnits = new[] { "kg", "lbs", "st", "g" };
        return validUnits.Contains(weightUnit.ToLowerInvariant());
    }

    private static bool BeValidHeightUnit(string? heightUnit)
    {
        if (string.IsNullOrEmpty(heightUnit)) return true;
        
        var validUnits = new[] { "cm", "in", "ft", "m" };
        return validUnits.Contains(heightUnit.ToLowerInvariant());
    }
}

#region Additional DTOs for Validation

/// <summary>
/// DTO для записи веса (для валидации)
/// </summary>
public class WeightRecordDTO
{
    public decimal Weight { get; set; }
    public DateOnly RecordedDate { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO для записи еды (для валидации)
/// </summary>
public class FoodEntryDTO
{
    public string FoodName { get; set; } = string.Empty;
    public int Calories { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateOnly EntryDate { get; set; }
    public string MealType { get; set; } = string.Empty;
}

/// <summary>
/// DTO для настроек пользователя (для валидации)
/// </summary>
public class UserPreferencesDTO
{
    public bool EmailNotifications { get; set; } = true;
    public bool DailyReminder { get; set; } = true;
    public bool WeeklySummary { get; set; } = true;
    public bool ProfilePublic { get; set; } = false;
    public bool StatsPublic { get; set; } = false;
    public string? Language { get; set; } = "ru";
    public string? Timezone { get; set; } = "UTC";
    public string? DateFormat { get; set; } = "dd.MM.yyyy";
    public string? WeightUnit { get; set; } = "kg";
    public string? HeightUnit { get; set; } = "cm";
}

#endregion