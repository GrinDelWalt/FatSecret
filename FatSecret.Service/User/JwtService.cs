using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FatSecret.DAL.Interfaces;
using FatSecret.Domain.Entities.Identity;
using FatSecret.Service.Interfaces.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace FatSecret.Service.User;

public class JwtService : IJwtService
{
    private readonly ILogger<JwtService> _logger;
    private readonly IEntityRepository<UserToken> _tokenRepository;
    private readonly IEntityRepository<Domain.Entities.Identity.User> _userRepository;
    private readonly IConfiguration _configuration;

    public JwtService(
        IEntityRepository<UserToken> tokenRepository,
        IEntityRepository<Domain.Entities.Identity.User> userRepository,
        IConfiguration configuration, 
        ILogger<JwtService> logger)
    {
        _logger = logger;
        _tokenRepository = tokenRepository;
        _userRepository = userRepository;
        _configuration = configuration;
    }

    public async Task<string> GenerateTokenAsync(string email)
    {
        // Поиск пользователя
        var user = await _userRepository.All()
            .Where(x => x.Email == email)
            .FirstOrDefaultAsync();

        if (user == null)
        {
            _logger.LogError("User {Login} not found", email);
            throw new UnauthorizedAccessException("Пользователь не найден.");
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]); 

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("UserId", user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email)
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        // Сохраняем данные токена в базе данных
        var userToken = new UserToken 
        { 
            UserId = user.Id, 
            Token = tokenString,
            Value = tokenString, // Дублируем для совместимости
            HashedToken = ComputeHash(tokenString), // Хешируем для безопасности
            ExpirationAt = tokenDescriptor.Expires
        };

        _tokenRepository.Add(userToken);
        await _tokenRepository.SaveChangesAsync();

        _logger.LogInformation("Token generated for user {email}", email);

        return tokenString;
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

            // Проверяем, что токен не отозван
            var hashedToken = ComputeHash(token);
            var tokenExists = await _tokenRepository.All()
                .AnyAsync(t => t.HashedToken == hashedToken && t.ExpirationAt > DateTimeOffset.UtcNow);

            return tokenExists;
        }
        catch
        {
            return false;
        }
    }

    public async Task RevokeTokenAsync(string token)
    {
        var hashedToken = ComputeHash(token);
        var userToken = await _tokenRepository.All()
            .FirstOrDefaultAsync(t => t.HashedToken == hashedToken);

        if (userToken != null)
        {
            _tokenRepository.Delete(userToken);
            await _tokenRepository.SaveChangesAsync();
            _logger.LogInformation("Token revoked successfully");
        }
    }

    private static string ComputeHash(string input)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(hashedBytes);
    }
}