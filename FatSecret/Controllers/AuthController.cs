using FatSecret.Domain.Models.API;
using FatSecret.Domain.Models.DTO;
using FatSecret.Filters;
using FatSecret.Service.Interfaces.Authentication;
using FatSecret.Service.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;

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

    public AuthController(
        CreateUser createUserService,
        IAuthService authService,
        ILogger<AuthController> logger,
        IValidator<CreateUserRequestsDTO> createUserValidator,
        IValidator<LoginRequestDTO> loginValidator)
    {
        _createUserService = createUserService;
        _authService = authService;
        _logger = logger;
        _createUserValidator = createUserValidator;
        _loginValidator = loginValidator;
    }

    /// <summary>
    /// Регистрация нового пользователя
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<CreateUserResponseDTO>>> Register(
        [FromBody] CreateUserRequestsDTO request)
    {
        // Валидация запроса
        var validationResult = await _createUserValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(new ApiResponse<CreateUserResponseDTO>
            {
                Success = false,
                Message = string.Join("; ", errors)
            });
        }

        var result = await _createUserService.Execute(request);
        
        return Ok(new ApiResponse<CreateUserResponseDTO>
        {
            Success = true,
            Data = result,
            Message = "Пользователь успешно зарегистрирован"
        });
    }

    /// <summary>
    /// Аутентификация пользователя
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponseDTO>>> Login(
        [FromBody] LoginRequestDTO request)
    {
        // Валидация запроса
        var validationResult = await _loginValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(new ApiResponse<LoginResponseDTO>
            {
                Success = false,
                Message = string.Join("; ", errors)
            });
        }

        var token = await _authService.LoginAsync(request.Login, request.Password);
        
        return Ok(new ApiResponse<LoginResponseDTO>
        {
            Success = true,
            Data = new LoginResponseDTO(token, request.Login, ""),
            Message = "Аутентификация прошла успешно"
        });
    }

    /// <summary>
    /// Выход пользователя (отзыв токена)
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> Logout()
    {
        var token = HttpContext.Request.Headers["Authorization"]
            .FirstOrDefault()?.Split(" ").Last();

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

    /// <summary>
    /// Получение информации о текущем пользователе
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public ActionResult<ApiResponse<UserInformationDTO>> GetCurrentUser()
    {
        var login = User.Identity?.Name;
        var email = User.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? "";

        if (string.IsNullOrEmpty(login))
        {
            return Unauthorized(new ApiResponse<UserInformationDTO>
            {
                Success = false,
                Message = "Пользователь не аутентифицирован"
            });
        }

        return Ok(new ApiResponse<UserInformationDTO>
        {
            Success = true,
            Data = new UserInformationDTO(email, login),
            Message = "Информация о пользователе получена успешно"
        });
    }
}