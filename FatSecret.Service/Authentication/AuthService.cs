using FatSecret.Service.Interfaces.Authentication;
using FatSecret.Service.Interfaces.Password;
using FatSecret.Service.Interfaces.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FatSecret.Service.Authentication;
public class AuthService : IAuthService
{
    private readonly IEntityRepository<Domain.Entities.Identity.User> _userRepository;
    private readonly IPasswordService _passwordService;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IEntityRepository<Domain.Entities.Identity.User> userRepository,
        IPasswordService passwordService,
        IJwtService jwtService,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _passwordService = passwordService;
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task<string> LoginAsync(string login, string password)
    {
        // Найти пользователя по логину или email
        var user = await _userRepository.All()
            .Where(x => x.Login == login || x.Email == login.ToLowerInvariant())
            .FirstOrDefaultAsync();

        if (user == null)
        {
            _logger.LogWarning("Login attempt with non-existing user: {Login}", login);
            throw new UnauthorizedAccessException("Неверный логин или пароль");
        }

        // Проверить пароль
        if (!_passwordService.VerifyPassword(password, user.Password))
        {
            _logger.LogWarning("Invalid password for user: {Login}", login);
            throw new UnauthorizedAccessException("Неверный логин или пароль");
        }

        // Генерировать JWT токен
        var token = await _jwtService.GenerateTokenAsync(user.Login);

        _logger.LogInformation("User {Login} logged in successfully", user.Login);

        return token;
    }

    public async Task LogoutAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return;

        await _jwtService.RevokeTokenAsync(token);
        _logger.LogInformation("User logged out, token revoked");
    }
}