using FluentValidation;
using FatSecret.Domain.Models.DTO;
using FatSecret.Domain.Models.DTO.User;
using Microsoft.AspNetCore.Mvc;
using static FatSecret.Validation.ValidationConstants;
using static FatSecret.Validation.ValidationMessages;

namespace FatSecret.Validation.Improved;

/// <summary>
/// Улучшенный валидатор для регистрации пользователя
/// </summary>
public class ImprovedCreateUserRequestValidator : AbstractValidator<CreateUserRequestsDTO>
{
    private readonly IServiceProvider _serviceProvider;

    public ImprovedCreateUserRequestValidator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        ConfigureValidationRules();
    }

    private void ConfigureValidationRules()
    {
        // Имя пользователя
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage(REQUIRED)
            .Length(MIN_USERNAME_LENGTH, MAX_USERNAME_LENGTH)
            .WithMessage($"Имя пользователя должно содержать от {MIN_USERNAME_LENGTH} до {MAX_USERNAME_LENGTH} символов")
            .UsernameFormat()
            .MustAsync(async (username, cancellation) => 
                await CustomValidators.BeUniqueUsername(_serviceProvider)(username, null, cancellation))
            .WithMessage("Пользователь с таким именем уже существует");

        // Email
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(REQUIRED)
            .EmailAddress().WithMessage(INVALID_EMAIL)
            .MaximumLength(MAX_EMAIL_LENGTH).WithMessage($"Email не может быть длиннее {MAX_EMAIL_LENGTH} символов")
            .MustAsync(async (email, cancellation) => 
                await CustomValidators.BeUniqueEmail(_serviceProvider)(email, null, cancellation))
            .WithMessage("Пользователь с таким email уже существует");

        // Пароль
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(REQUIRED)
            .Length(MIN_PASSWORD_LENGTH, MAX_PASSWORD_LENGTH)
            .WithMessage($"Пароль должен содержать от {MIN_PASSWORD_LENGTH} до {MAX_PASSWORD_LENGTH} символов")
            .Must(password => !string.IsNullOrEmpty(password) && password.Any(char.IsLetter))
            .WithMessage(WEAK_PASSWORD);

        // Физические параметры (условная валидация)
        When(x => x.Age > 0, () => {
            RuleFor(x => x.Age)
                .InclusiveBetween(MIN_AGE, MAX_AGE)
                .WithMessage(string.Format(INVALID_AGE, MIN_AGE, MAX_AGE));
        });

        When(x => x.Weight > 0, () => {
            RuleFor(x => x.Weight)
                .InclusiveBetween(MIN_WEIGHT, MAX_WEIGHT)
                .WithMessage(string.Format(INVALID_WEIGHT, MIN_WEIGHT, MAX_WEIGHT));
        });

        When(x => x.Height > 0, () => {
            RuleFor(x => x.Height)
                .InclusiveBetween(MIN_HEIGHT, MAX_HEIGHT)
                .WithMessage(string.Format(INVALID_HEIGHT, MIN_HEIGHT, MAX_HEIGHT));
        });

        // Пол (опционально)
        When(x => !string.IsNullOrEmpty(x.Gender), () => {
            RuleFor(x => x.Gender)
                .Must(gender => VALID_GENDERS.Contains(gender!.ToLowerInvariant()))
                .WithMessage(INVALID_GENDER);
        });

        // Цель (обязательно)
        RuleFor(x => x.Goal)
            .NotEmpty().WithMessage(REQUIRED)
            .Must(goal => VALID_GOALS.Contains(goal.ToLowerInvariant()))
            .WithMessage(INVALID_GOAL);

        // Уровень активности (обязательно)
        RuleFor(x => x.ActivityLevel)
            .NotEmpty().WithMessage(REQUIRED)
            .Must(level => VALID_ACTIVITY_LEVELS.Contains(level.ToLowerInvariant()))
            .WithMessage(INVALID_ACTIVITY_LEVEL);

        // Заметки (опционально)
        When(x => !string.IsNullOrEmpty(x.Notes), () => {
            RuleFor(x => x.Notes)
                .MaximumLength(MAX_NOTES_LENGTH)
                .WithMessage($"Заметки не могут быть длиннее {MAX_NOTES_LENGTH} символов");
        });

        // Проверка целостности данных
        RuleFor(x => x)
            .Must(HaveConsistentPhysicalData)
            .WithMessage("Для расчета BMR необходимо указать возраст, вес и рост")
            .When(x => x.Age > 0 || x.Weight > 0 || x.Height > 0);
    }

    private static bool HaveConsistentPhysicalData(CreateUserRequestsDTO dto)
    {
        // Если указан хотя бы один физический параметр, должны быть указаны все
        var hasAnyPhysicalData = dto.Age > 0 || dto.Weight > 0 || dto.Height > 0;
        if (!hasAnyPhysicalData) return true;

        return dto.Age > 0 && dto.Weight > 0 && dto.Height > 0;
    }
}

/// <summary>
/// Улучшенный валидатор для входа в систему
/// </summary>
public class ImprovedLoginRequestValidator : AbstractValidator<LoginRequestDTO>
{
    public ImprovedLoginRequestValidator()
    {
        RuleFor(x => x.Login)
            .NotEmpty().WithMessage(REQUIRED)
            .MaximumLength(MAX_EMAIL_LENGTH).WithMessage(TOO_LONG);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(REQUIRED)
            .MaximumLength(MAX_PASSWORD_LENGTH).WithMessage(TOO_LONG);
    }
}

/// <summary>
/// Улучшенный валидатор для обновления профиля
/// </summary>
public class ImprovedUpdateUserProfileValidator : AbstractValidator<UpdateUserProfileDTO>
{
    private readonly IServiceProvider _serviceProvider;

    public ImprovedUpdateUserProfileValidator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        ConfigureValidationRules();
    }

    private void ConfigureValidationRules()
    {
        // Имя пользователя (опционально при обновлении)
        When(x => !string.IsNullOrEmpty(x.Username), () => {
            RuleFor(x => x.Username)
                .Length(MIN_USERNAME_LENGTH, MAX_USERNAME_LENGTH)
                .WithMessage($"Имя пользователя должно содержать от {MIN_USERNAME_LENGTH} до {MAX_USERNAME_LENGTH} символов")
                .UsernameFormat();
            // Примечание: Проверка уникальности должна выполняться в сервисе с учетом текущего пользователя
        });

        // Email (опционально при обновлении)
        When(x => !string.IsNullOrEmpty(x.Email), () => {
            RuleFor(x => x.Email)
                .EmailAddress().WithMessage(INVALID_EMAIL)
                .MaximumLength(MAX_EMAIL_LENGTH).WithMessage($"Email не может быть длиннее {MAX_EMAIL_LENGTH} символов");
            // Примечание: Проверка уникальности должна выполняться в сервисе с учетом текущего пользователя
        });

        // Физические параметры
        When(x => x.Age.HasValue, () => {
            RuleFor(x => x.Age)
                .InclusiveBetween(MIN_AGE, MAX_AGE)
                .WithMessage(string.Format(INVALID_AGE, MIN_AGE, MAX_AGE));
        });

        When(x => x.Weight.HasValue, () => {
            RuleFor(x => x.Weight)
                .InclusiveBetween(MIN_WEIGHT, MAX_WEIGHT)
                .WithMessage(string.Format(INVALID_WEIGHT, MIN_WEIGHT, MAX_WEIGHT));
        });

        When(x => x.Height.HasValue, () => {
            RuleFor(x => x.Height)
                .InclusiveBetween(MIN_HEIGHT, MAX_HEIGHT)
                .WithMessage(string.Format(INVALID_HEIGHT, MIN_HEIGHT, MAX_HEIGHT));
        });

        // Пол
        When(x => !string.IsNullOrEmpty(x.Gender), () => {
            RuleFor(x => x.Gender)
                .Must(gender => VALID_GENDERS.Contains(gender!.ToLowerInvariant()))
                .WithMessage(INVALID_GENDER);
        });

        // Цель
        When(x => !string.IsNullOrEmpty(x.Goal), () => {
            RuleFor(x => x.Goal)
                .Must(goal => VALID_GOALS.Contains(goal!.ToLowerInvariant()))
                .WithMessage(INVALID_GOAL);
        });

        // Уровень активности
        When(x => !string.IsNullOrEmpty(x.ActivityLevel), () => {
            RuleFor(x => x.ActivityLevel)
                .Must(level => VALID_ACTIVITY_LEVELS.Contains(level!.ToLowerInvariant()))
                .WithMessage(INVALID_ACTIVITY_LEVEL);
        });

        // Заметки
        When(x => x.Notes != null, () => {
            RuleFor(x => x.Notes)
                .MaximumLength(MAX_NOTES_LENGTH)
                .WithMessage($"Заметки не могут быть длиннее {MAX_NOTES_LENGTH} символов");
        });
    }
}

/// <summary>
/// Улучшенный валидатор для смены пароля
/// </summary>
public class ImprovedChangePasswordValidator : AbstractValidator<ChangePasswordDTO>
{
    public ImprovedChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage(REQUIRED)
            .MaximumLength(MAX_PASSWORD_LENGTH).WithMessage(TOO_LONG);

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage(REQUIRED)
            .Length(MIN_PASSWORD_LENGTH, MAX_PASSWORD_LENGTH)
            .WithMessage($"Пароль должен содержать от {MIN_PASSWORD_LENGTH} до {MAX_PASSWORD_LENGTH} символов")
            .Must(password => !string.IsNullOrEmpty(password) && password.Any(char.IsLetter))
            .WithMessage(WEAK_PASSWORD)
            .NotEqual(x => x.CurrentPassword)
            .WithMessage("Новый пароль должен отличаться от текущего");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage(REQUIRED)
            .Equal(x => x.NewPassword).WithMessage(PASSWORDS_NOT_MATCH);
    }
}

/// <summary>
/// Улучшенный валидатор для записи веса
/// </summary>
public class ImprovedWeightRecordValidator : AbstractValidator<WeightRecordDTO>
{
    public ImprovedWeightRecordValidator()
    {
        RuleFor(x => x.Weight)
            .InclusiveBetween(MIN_WEIGHT, MAX_WEIGHT)
            .WithMessage(string.Format(INVALID_WEIGHT, MIN_WEIGHT, MAX_WEIGHT));

        RuleFor(x => x.RecordedDate)
            .NotEmpty().WithMessage(REQUIRED)
            .NotInFuture().WithMessage(FUTURE_DATE);

        When(x => !string.IsNullOrEmpty(x.Notes), () => {
            RuleFor(x => x.Notes)
                .MaximumLength(MAX_NOTES_LENGTH)
                .WithMessage($"Заметки не могут быть длиннее {MAX_NOTES_LENGTH} символов");
        });
    }
}

/// <summary>
/// Улучшенный валидатор для записи еды
/// </summary>
public class ImprovedFoodEntryValidator : AbstractValidator<FoodEntryDTO>
{
    public ImprovedFoodEntryValidator()
    {
        RuleFor(x => x.FoodName)
            .NotEmpty().WithMessage(REQUIRED)
            .MaximumLength(MAX_FOOD_NAME_LENGTH)
            .WithMessage($"Название еды не может быть длиннее {MAX_FOOD_NAME_LENGTH} символов");

        RuleFor(x => x.Calories)
            .InclusiveBetween(0, MAX_CALORIES)
            .WithMessage($"Калории должны быть от 0 до {MAX_CALORIES}");

        RuleFor(x => x.Quantity)
            .InclusiveBetween(0.01m, MAX_QUANTITY)
            .WithMessage($"Количество должно быть от 0.01 до {MAX_QUANTITY}");

        RuleFor(x => x.Unit)
            .NotEmpty().WithMessage(REQUIRED)
            .MaximumLength(50).WithMessage("Единица измерения не может быть длиннее 50 символов");

        RuleFor(x => x.EntryDate)
            .NotEmpty().WithMessage(REQUIRED)
            .NotInFuture().WithMessage(FUTURE_DATE);

        RuleFor(x => x.MealType)
            .NotEmpty().WithMessage(REQUIRED)
            .Must(mealType => VALID_MEAL_TYPES.Contains(mealType.ToLowerInvariant()))
            .WithMessage(INVALID_MEAL_TYPE);
    }
}

/// <summary>
/// Улучшенный валидатор для настроек пользователя
/// </summary>
public class ImprovedUserPreferencesValidator : AbstractValidator<UserPreferencesDTO>
{
    public ImprovedUserPreferencesValidator()
    {
        When(x => !string.IsNullOrEmpty(x.Language), () => {
            RuleFor(x => x.Language)
                .MaximumLength(10).WithMessage("Код языка не может быть длиннее 10 символов")
                .Must(lang => VALID_LANGUAGES.Contains(lang!.ToLowerInvariant()))
                .WithMessage($"Недопустимый код языка. Допустимые значения: {string.Join(", ", VALID_LANGUAGES)}");
        });

        When(x => !string.IsNullOrEmpty(x.Timezone), () => {
            RuleFor(x => x.Timezone)
                .MaximumLength(50).WithMessage("Часовой пояс не может быть длиннее 50 символов")
                .Must(BeValidTimezone).WithMessage("Недопустимый часовой пояс");
        });

        When(x => !string.IsNullOrEmpty(x.DateFormat), () => {
            RuleFor(x => x.DateFormat)
                .MaximumLength(20).WithMessage("Формат даты не может быть длиннее 20 символов")
                .Must(format => VALID_DATE_FORMATS.Contains(format!))
                .WithMessage($"Недопустимый формат даты. Допустимые значения: {string.Join(", ", VALID_DATE_FORMATS)}");
        });

        When(x => !string.IsNullOrEmpty(x.WeightUnit), () => {
            RuleFor(x => x.WeightUnit)
                .MaximumLength(10).WithMessage("Единица веса не может быть длиннее 10 символов")
                .Must(unit => VALID_WEIGHT_UNITS.Contains(unit!.ToLowerInvariant()))
                .WithMessage($"Недопустимая единица веса. Допустимые значения: {string.Join(", ", VALID_WEIGHT_UNITS)}");
        });

        When(x => !string.IsNullOrEmpty(x.HeightUnit), () => {
            RuleFor(x => x.HeightUnit)
                .MaximumLength(10).WithMessage("Единица роста не может быть длиннее 10 символов")
                .Must(unit => VALID_HEIGHT_UNITS.Contains(unit!.ToLowerInvariant()))
                .WithMessage($"Недопустимая единица роста. Допустимые значения: {string.Join(", ", VALID_HEIGHT_UNITS)}");
        });
    }

    private static bool BeValidTimezone(string timezone)
    {
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timezone);
            return true;
        }
        catch
        {
            // Также проверяем стандартные UTC форматы
            return timezone == "UTC" || 
                   timezone.StartsWith("UTC+") || 
                   timezone.StartsWith("UTC-") ||
                   timezone.StartsWith("GMT+") || 
                   timezone.StartsWith("GMT-");
        }
    }
}

/// <summary>
/// Валидатор для пакетных операций
/// </summary>
public class BatchOperationValidator<T> : AbstractValidator<IEnumerable<T>>
{
    public BatchOperationValidator(IValidator<T> itemValidator, int maxItems = 100)
    {
        RuleFor(x => x)
            .NotNull().WithMessage("Список не может быть null")
            .NotEmpty().WithMessage("Список не может быть пустым")
            .Must(items => items.Count() <= maxItems)
            .WithMessage($"Максимальное количество элементов: {maxItems}");

        RuleForEach(x => x).SetValidator(itemValidator);
    }
}

/// <summary>
/// Пример использования валидаторов в контроллере
/// </summary>
public static class ValidatorUsageExample
{
    public static async Task<IActionResult> ExampleUsage(
        CreateUserRequestsDTO request,
        IValidator<CreateUserRequestsDTO> validator)
    {
        // Ручная валидация
        var validationResult = await validator.ValidateAsync(request);
        
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage);
            return new BadRequestObjectResult(ValidationHelper.CreateValidationErrorResponse<object>(errors));
        }

        // Продолжаем обработку...
        return new OkResult();
    }
}