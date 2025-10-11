using FatSecret.Domain.Models.API;
using FatSecret.Domain.Models.DTO;
using FatSecret.Domain.Models.DTO.User;
using FatSecret.Service.Interfaces.Authentication;
using FatSecret.Service.User;
using FatSecret.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using ValidationFilterAttribute = FatSecret.Filters.ValidationFilterAttribute;

namespace FatSecret.Controllers;

[Route("api/[controller]")]
[ApiController]
[ServiceFilter(typeof(ValidationFilterAttribute))]
public class AuthController : ControllerBase
{
    private readonly CreateUser _createUserService;
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;
    private readonly IValidator<CreateUserRequestsDTO> _createUserValidator;
    private readonly IValidator<LoginRequestDTO> _loginValidator;
    private readonly IValidator<UpdateUserProfileDTO> _updateProfileValidator;

    public AuthController(
        CreateUser createUserService,
        IAuthService authService,
        ILogger<AuthController> logger,
        IValidator<CreateUserRequestsDTO> createUserValidator,
        IValidator<LoginRequestDTO> loginValidator,
        IValidator<UpdateUserProfileDTO> updateProfileValidator)
    {
        _createUserService = createUserService;
        _authService = authService;
        _logger = logger;
        _createUserValidator = createUserValidator;
        _loginValidator = loginValidator;
        _updateProfileValidator = updateProfileValidator;
    }

    /// <summary>
    /// Регистрация нового пользователя с расширенными данными
    /// </summary>
    [HttpPost("register")]
    [ServiceFilter(typeof(ValidationFilterAttribute))] // Автоматическая валидация
    public async Task<ActionResult<ApiResponse<CreateUserResponseDTO>>> Register(
        [FromBody] CreateUserRequestsDTO request)
    {
        // Валидация уже выполнена автоматически!
        var result = await _createUserService.Execute(request);
        return Ok(ValidationHelper.CreateSuccessResponse(result));
    }

    /// <summary>
    /// Аутентификация пользователя
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponseDTO>>> Login(
        [FromBody] LoginRequestDTO request)
    {
        try
        {
            _logger.LogInformation("Попытка входа пользователя: {Login}", request.Login);

            // Валидация запроса
            var validationResult = await _loginValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("Ошибка валидации при входе: {Errors}", string.Join("; ", errors));
                
                return BadRequest(new ApiResponse<LoginResponseDTO>
                {
                    Success = false,
                    Message = string.Join("; ", errors)
                });
            }

            var loginResult = await _authService.LoginAsync(request.Login, request.Password);
            
            _logger.LogInformation("Пользователь успешно вошел в систему: {Login}", request.Login);

            return Ok(new ApiResponse<LoginResponseDTO>
            {
                Success = true,
                Data = loginResult,
                Message = "Аутентификация прошла успешно"
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Неудачная попытка входа: {Login}, Причина: {Message}", request.Login, ex.Message);
            return Unauthorized(new ApiResponse<LoginResponseDTO>
            {
                Success = false,
                Message = "Неверные учетные данные"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Неожиданная ошибка при входе пользователя: {Login}", request.Login);
            return StatusCode(500, new ApiResponse<LoginResponseDTO>
            {
                Success = false,
                Message = "Произошла внутренняя ошибка сервера"
            });
        }
    }

    /// <summary>
    /// Выход пользователя (отзыв токена)
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> Logout()
    {
        try
        {
            var token = HttpContext.Request.Headers["Authorization"]
                .FirstOrDefault()?.Split(" ").Last();

            var userLogin = User.Identity?.Name;
            _logger.LogInformation("Выход пользователя: {Login}", userLogin);

            if (!string.IsNullOrEmpty(token))
            {
                await _authService.LogoutAsync(token);
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Выход выполнен успешно"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при выходе пользователя");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Произошла ошибка при выходе"
            });
        }
    }

    /// <summary>
    /// Получение расширенной информации о текущем пользователе
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserProfileDTO>>> GetCurrentUser()
    {
        try
        {
            var userLogin = User.Identity?.Name;
            
            if (string.IsNullOrEmpty(userLogin))
            {
                return Unauthorized(new ApiResponse<UserProfileDTO>
                {
                    Success = false,
                    Message = "Пользователь не аутентифицирован"
                });
            }

            var userProfile = await _authService.GetUserProfileAsync(userLogin);

            return Ok(new ApiResponse<UserProfileDTO>
            {
                Success = true,
                Data = userProfile,
                Message = "Информация о пользователе получена успешно"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении профиля пользователя");
            return StatusCode(500, new ApiResponse<UserProfileDTO>
            {
                Success = false,
                Message = "Произошла ошибка при получении профиля"
            });
        }
    }

    /// <summary>
    /// Обновление профиля пользователя
    /// </summary>
    [HttpPut("profile")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserProfileDTO>>> UpdateProfile(
        [FromBody] UpdateUserProfileDTO request)
    {
        try
        {
            var userLogin = User.Identity?.Name;
            
            if (string.IsNullOrEmpty(userLogin))
            {
                return Unauthorized(new ApiResponse<UserProfileDTO>
                {
                    Success = false,
                    Message = "Пользователь не аутентифицирован"
                });
            }

            // Валидация запроса
            var validationResult = await _updateProfileValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(new ApiResponse<UserProfileDTO>
                {
                    Success = false,
                    Message = string.Join("; ", errors)
                });
            }

            // Пересчитываем BMR и целевые калории если изменились физические параметры
            if (request.Age.HasValue && request.Weight.HasValue && request.Height.HasValue)
            {
                request.BasalMetabolicRate = CalculateBMR(
                    request.Age.Value, 
                    request.Weight.Value, 
                    request.Height.Value, 
                    request.Gender ?? "unknown");
                    
                request.DailyCalorieTarget = CalculateTargetCalories(
                    request.BasalMetabolicRate, 
                    request.ActivityLevel ?? "moderate", 
                    request.Goal ?? "maintain");
            }

            var updatedProfile = await _authService.UpdateUserProfileAsync(userLogin, request);

            _logger.LogInformation("Профиль пользователя обновлен: {Login}", userLogin);

            return Ok(new ApiResponse<UserProfileDTO>
            {
                Success = true,
                Data = updatedProfile,
                Message = "Профиль успешно обновлен"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обновлении профиля пользователя");
            return StatusCode(500, new ApiResponse<UserProfileDTO>
            {
                Success = false,
                Message = "Произошла ошибка при обновлении профиля"
            });
        }
    }

    /// <summary>
    /// Смена пароля
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword(
        [FromBody] ChangePasswordDTO request)
    {
        try
        {
            var userLogin = User.Identity?.Name;
            
            if (string.IsNullOrEmpty(userLogin))
            {
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Пользователь не аутентифицирован"
                });
            }

            await _authService.ChangePasswordAsync(userLogin, request.CurrentPassword, request.NewPassword);

            _logger.LogInformation("Пароль изменен для пользователя: {Login}", userLogin);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Пароль успешно изменен"
            });
        }
        catch (UnauthorizedAccessException)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Неверный текущий пароль"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при смене пароля");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Произошла ошибка при смене пароля"
            });
        }
    }

    #region Private Methods

    /// <summary>
    /// Расчет базального метаболизма по формуле Миффлина-Сан Жеора
    /// </summary>
    private static int CalculateBMR(int age, decimal weight, int height, string gender)
    {
        // Формула Миффлина-Сан Жеора
        decimal bmr = 10 * weight + 6.25m * height - 5 * age;
        
        // Корректировка по полу
        bmr += gender?.ToLower() switch
        {
            "male" or "м" or "мужской" => 5,
            "female" or "ж" or "женский" => -161,
            _ => -78 // Усредненное значение если пол не указан
        };

        return (int)Math.Round(bmr);
    }

    /// <summary>
    /// Расчет целевых калорий с учетом активности и цели
    /// </summary>
    private static int CalculateTargetCalories(int bmr, string activityLevel, string goal)
    {
        // Коэффициенты активности
        var activityMultiplier = activityLevel?.ToLower() switch
        {
            "low" or "низкая" => 1.2m,
            "moderate" or "умеренная" => 1.375m,
            "high" or "высокая" => 1.55m,
            "very-high" or "очень-высокая" => 1.725m,
            _ => 1.375m // По умолчанию умеренная активность
        };

        var totalCalories = bmr * activityMultiplier;

        // Корректировка по цели
        var adjustment = goal?.ToLower() switch
        {
            "lose" or "похудение" => -500,
            "gain" or "набор" => 500,
            "maintain" or "поддержание" => 0,
            _ => 0
        };

        return (int)Math.Round(totalCalories + adjustment);
    }

    #endregion
}