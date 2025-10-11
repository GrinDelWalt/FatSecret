using System.Text.Json;
using FatSecret.DAL.Context;
using FatSecret.Domain.Models.API;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FatSecret.Validation;

/// <summary>
/// Расширения для FluentValidation
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Проверка на содержание только букв и цифр
    /// </summary>
    public static IRuleBuilderOptions<T, string> AlphaNumericOnly<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.Matches(@"^[a-zA-Z0-9]+$").WithMessage("Поле должно содержать только буквы и цифры");
    }

    /// <summary>
    /// Проверка на содержание только букв, цифр и подчеркиваний
    /// </summary>
    public static IRuleBuilderOptions<T, string> UsernameFormat<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.Matches(@"^[a-zA-Z0-9_а-яА-Я]+$")
            .WithMessage("Имя пользователя может содержать только буквы, цифры и подчеркивания");
    }

    /// <summary>
    /// Проверка силы пароля
    /// </summary>
    public static IRuleBuilderOptions<T, string> StrongPassword<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.Must(BeStrongPassword)
            .WithMessage("Пароль должен содержать хотя бы одну заглавную букву, одну строчную букву, одну цифру и один специальный символ");
    }

    /// <summary>
    /// Проверка на валидный URL
    /// </summary>
    public static IRuleBuilderOptions<T, string> ValidUrl<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.Must(BeValidUrl).WithMessage("Некорректный формат URL");
    }

    /// <summary>
    /// Проверка на валидный номер телефона
    /// </summary>
    public static IRuleBuilderOptions<T, string> PhoneNumber<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.Matches(@"^\+?[1-9]\d{1,14}$")
            .WithMessage("Некорректный формат номера телефона");
    }

    /// <summary>
    /// Проверка на минимальный возраст
    /// </summary>
    public static IRuleBuilderOptions<T, DateTime> MinimumAge<T>(this IRuleBuilder<T, DateTime> ruleBuilder, int minimumAge)
    {
        return ruleBuilder.Must(birthDate => CalculateAge(birthDate) >= minimumAge)
            .WithMessage($"Минимальный возраст: {minimumAge} лет");
    }

    /// <summary>
    /// Проверка на дату не в будущем
    /// </summary>
    public static IRuleBuilderOptions<T, DateTime> NotInFuture<T>(this IRuleBuilder<T, DateTime> ruleBuilder)
    {
        return ruleBuilder.LessThanOrEqualTo(DateTime.Today)
            .WithMessage("Дата не может быть в будущем");
    }

    /// <summary>
    /// Проверка на дату не в будущем (DateOnly)
    /// </summary>
    public static IRuleBuilderOptions<T, DateOnly> NotInFuture<T>(this IRuleBuilder<T, DateOnly> ruleBuilder)
    {
        return ruleBuilder.LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("Дата не может быть в будущем");
    }

    #region Private Methods

    private static bool BeStrongPassword(string password)
    {
        if (string.IsNullOrEmpty(password)) return false;

        var hasUpper = password.Any(char.IsUpper);
        var hasLower = password.Any(char.IsLower);
        var hasDigit = password.Any(char.IsDigit);
        var hasSpecial = password.Any(ch => !char.IsLetterOrDigit(ch));

        return hasUpper && hasLower && hasDigit && hasSpecial;
    }

    private static bool BeValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var result)
               && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }

    private static int CalculateAge(DateTime birthDate)
    {
        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
        return age;
    }

    #endregion
}

/// <summary>
/// Кастомные валидаторы для специфических случаев
/// </summary>
public static class CustomValidators
{
    /// <summary>
    /// Проверка уникальности email в базе данных
    /// </summary>
    public static Func<string, int?, CancellationToken, Task<bool>> BeUniqueEmail(IServiceProvider serviceProvider)
    {
        return async (email, excludeUserId, cancellationToken) =>
        {
            using var scope = serviceProvider.CreateScope();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            return await userRepository.IsEmailUniqueAsync(email, excludeUserId);
        };
    }

    /// <summary>
    /// Проверка уникальности имени пользователя в базе данных
    /// </summary>
    public static Func<string, int?, CancellationToken, Task<bool>> BeUniqueUsername(IServiceProvider serviceProvider)
    {
        return async (username, excludeUserId, cancellationToken) =>
        {
            using var scope = serviceProvider.CreateScope();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            return await userRepository.IsUsernameUniqueAsync(username, excludeUserId);
        };
    }

    /// <summary>
    /// Проверка существования пользователя
    /// </summary>
    public static Func<string, CancellationToken, Task<bool>> UserExists(IServiceProvider serviceProvider)
    {
        return async (login, cancellationToken) =>
        {
            using var scope = serviceProvider.CreateScope();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var user = await userRepository.GetByUsernameOrEmailAsync(login);
            return user != null;
        };
    }
}

/// <summary>
/// Атрибут фильтра для автоматической валидации
/// </summary>
public class ValidationFilterAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .SelectMany(x => x.Value?.Errors ?? new ModelErrorCollection())
                .Select(x => x.ErrorMessage)
                .ToList();

            var response = new ApiResponse<object>
            {
                Success = false,
                Message = string.Join("; ", errors),
                Data = null
            };

            context.Result = new BadRequestObjectResult(response);
        }

        base.OnActionExecuting(context);
    }
}

/// <summary>
/// Middleware для глобальной обработки ошибок валидации
/// </summary>
public class ValidationExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ValidationExceptionMiddleware> _logger;

    public ValidationExceptionMiddleware(RequestDelegate next, ILogger<ValidationExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("Ошибка валидации: {Errors}", string.Join("; ", ex.Errors.Select(e => e.ErrorMessage)));
            await HandleValidationExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Неожиданная ошибка");
            await HandleGenericExceptionAsync(context, ex);
        }
    }

    private static async Task HandleValidationExceptionAsync(HttpContext context, ValidationException ex)
    {
        var response = new ApiResponse<object>
        {
            Success = false,
            Message = "Ошибка валидации данных",
            Data = ex.Errors.Select(e => new { Field = e.PropertyName, Message = e.ErrorMessage })
        };

        context.Response.StatusCode = 400;
        context.Response.ContentType = "application/json";

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }

    private static async Task HandleGenericExceptionAsync(HttpContext context, Exception ex)
    {
        var response = new ApiResponse<object>
        {
            Success = false,
            Message = "Произошла внутренняя ошибка сервера",
            Data = null
        };

        // В разработке показываем детали ошибки
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
            response.Message = ex.Message;
            response.Data = new { StackTrace = ex.StackTrace };
        }

        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}

/// <summary>
/// Расширения для регистрации валидации в DI
/// </summary>
public static class ValidationServiceExtensions
{
    /// <summary>
    /// Добавление всех валидаторов
    /// </summary>
    public static IServiceCollection AddCustomValidation(this IServiceCollection services)
    {
        // Регистрация всех валидаторов из сборки
        services.AddValidatorsFromAssembly(typeof(CreateUserRequestValidator).Assembly);

        // Регистрация фильтра валидации
        services.AddScoped<ValidationFilterAttribute>();

        // Настройка поведения валидации
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.SuppressModelStateInvalidFilter = true; // Отключаем автоматическую валидацию
        });

        return services;
    }

    /// <summary>
    /// Добавление middleware для обработки ошибок валидации
    /// </summary>
    public static IApplicationBuilder UseValidationExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ValidationExceptionMiddleware>();
    }
}

/// <summary>
/// Хелперы для работы с валидацией
/// </summary>
public static class ValidationHelper
{
    /// <summary>
    /// Проверка валидности объекта
    /// </summary>
    public static async Task<(bool IsValid, IEnumerable<string> Errors)> ValidateAsync<T>(
        T model, 
        IValidator<T> validator)
    {
        var result = await validator.ValidateAsync(model);
        var errors = result.Errors.Select(e => e.ErrorMessage);
        return (result.IsValid, errors);
    }

    /// <summary>
    /// Создание стандартного ответа об ошибке валидации
    /// </summary>
    public static ApiResponse<T> CreateValidationErrorResponse<T>(IEnumerable<string> errors)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = string.Join("; ", errors),
            Data = default
        };
    }

    /// <summary>
    /// Создание ответа об успешной валидации
    /// </summary>
    public static ApiResponse<T> CreateSuccessResponse<T>(T data, string message = "Операция выполнена успешно")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }
}

/// <summary>
/// Константы для валидации
/// </summary>
public static class ValidationConstants
{
    public const int MIN_USERNAME_LENGTH = 3;
    public const int MAX_USERNAME_LENGTH = 50;
    public const int MIN_PASSWORD_LENGTH = 6;
    public const int MAX_PASSWORD_LENGTH = 100;
    public const int MAX_EMAIL_LENGTH = 255;
    public const int MAX_NOTES_LENGTH = 500;
    public const int MAX_FOOD_NAME_LENGTH = 200;
    
    public const int MIN_AGE = 1;
    public const int MAX_AGE = 120;
    public const decimal MIN_WEIGHT = 1;
    public const decimal MAX_WEIGHT = 1000;
    public const int MIN_HEIGHT = 1;
    public const int MAX_HEIGHT = 300;
    
    public const int MAX_CALORIES = 10000;
    public const decimal MAX_QUANTITY = 10000;

    public static readonly string[] VALID_GENDERS = { "male", "female", "other" };
    public static readonly string[] VALID_GOALS = { "lose", "maintain", "gain" };
    public static readonly string[] VALID_ACTIVITY_LEVELS = { "low", "moderate", "high", "very-high" };
    public static readonly string[] VALID_MEAL_TYPES = { "breakfast", "lunch", "dinner", "snack", "other" };
    public static readonly string[] VALID_LANGUAGES = { "ru", "en", "es", "fr", "de" };
    public static readonly string[] VALID_WEIGHT_UNITS = { "kg", "lbs", "st" };
    public static readonly string[] VALID_HEIGHT_UNITS = { "cm", "in", "ft" };
    public static readonly string[] VALID_DATE_FORMATS = { "dd.MM.yyyy", "MM/dd/yyyy", "yyyy-MM-dd" };
}

/// <summary>
/// Регулярные выражения для валидации
/// </summary>
public static class ValidationRegex
{
    public const string USERNAME_PATTERN = @"^[a-zA-Z0-9_а-яА-Я]+$";
    public const string PHONE_PATTERN = @"^\+?[1-9]\d{1,14}$";
    public const string STRONG_PASSWORD_PATTERN = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]";
    public const string ALPHANUMERIC_PATTERN = @"^[a-zA-Z0-9]+$";
    public const string LETTERS_ONLY_PATTERN = @"^[a-zA-Zа-яА-Я\s]+$";
    public const string NUMBERS_ONLY_PATTERN = @"^\d+$";
}

/// <summary>
/// Сообщения об ошибках валидации
/// </summary>
public static class ValidationMessages
{
    // Общие сообщения
    public const string REQUIRED = "Поле обязательно для заполнения";
    public const string INVALID_FORMAT = "Некорректный формат";
    public const string TOO_SHORT = "Значение слишком короткое";
    public const string TOO_LONG = "Значение слишком длинное";
    public const string OUT_OF_RANGE = "Значение вне допустимого диапазона";

    // Специфичные сообщения
    public const string INVALID_EMAIL = "Некорректный формат email адреса";
    public const string INVALID_USERNAME = "Имя пользователя может содержать только буквы, цифры и подчеркивания";
    public const string WEAK_PASSWORD = "Пароль должен содержать хотя бы одну букву";
    public const string PASSWORDS_NOT_MATCH = "Пароли не совпадают";
    public const string INVALID_AGE = "Возраст должен быть от {0} до {1} лет";
    public const string INVALID_WEIGHT = "Вес должен быть от {0} до {1} кг";
    public const string INVALID_HEIGHT = "Рост должен быть от {0} до {1} см";
    public const string FUTURE_DATE = "Дата не может быть в будущем";
    public const string INVALID_PHONE = "Некорректный формат номера телефона";
    public const string INVALID_URL = "Некорректный формат URL";

    // Сообщения для специфичных полей
    public const string INVALID_GOAL = "Недопустимое значение цели. Допустимые значения: lose, maintain, gain";
    public const string INVALID_ACTIVITY_LEVEL = "Недопустимый уровень активности. Допустимые значения: low, moderate, high, very-high";
    public const string INVALID_MEAL_TYPE = "Недопустимый тип приема пищи. Допустимые значения: breakfast, lunch, dinner, snack, other";
    public const string INVALID_GENDER = "Недопустимое значение пола. Допустимые значения: male, female, other";
}
