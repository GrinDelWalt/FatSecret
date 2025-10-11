using FatSecret.Domain.Models;
using FatSecret.Domain.Models.DTO;
using FatSecret.Service.Interfaces.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FatSecret.DAL.Context;
using FatSecret.Domain.Entities.Identity;
using FatSecret.Domain.Models.DTO.User;
using FatSecret.Service.Interfaces.Password;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FatSecret.Service.Authentication;

/// <summary>
/// Реализация сервиса аутентификации для расширенной модели пользователя
/// </summary>
public class AuthService : IAuthService
{
    private readonly FatSecretDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly IPasswordService _passwordService;

    public AuthService(
        FatSecretDbContext context,
        IConfiguration configuration,
        ILogger<AuthService> logger,
        IPasswordService passwordService)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _passwordService = passwordService;
    }

    public async Task<LoginResponseDTO> LoginAsync(string login, string password)
    {
        
        // Поиск пользователя по email или username
        var user = await _context.Users
            .FirstOrDefaultAsync(u => 
                (u.Email == login || u.Username == login) && 
                u.IsActive);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Пользователь не найден или неактивен");
        }
        
        _logger.LogDebug("Введенный пароль: {Password}", password);
        _logger.LogDebug("Хеш из БД: {Hash}", user.PasswordHash);
        
        // Проверка пароля
        if (!_passwordService.VerifyPassword(password, user.PasswordHash))
        {
            _logger.LogWarning("Invalid password for user {Login}", login);
            throw new UnauthorizedAccessException("Неверный пароль");
        }
        
        _logger.LogDebug("Вычисленный хеш: {user.PasswordSalt}", user.PasswordSalt);

        // Генерация JWT токена
        var token = await GenerateJwtTokenAsync(user);
        
        user.UpdateLastLogin();
        await _context.SaveChangesAsync();

        return new LoginResponseDTO(
            token.Token,
            user.Username,
            user.Email,
            token.ExpiresAt
        );
    }

    public async Task LogoutAsync(string token)
    {
        var tokenId = ExtractTokenId(token);
        if (string.IsNullOrEmpty(tokenId)) return;

        var session = await _context.UserSessions
            .FirstOrDefaultAsync(s => s.TokenId == tokenId && !s.IsRevoked);

        if (session != null)
        {
            session.Revoke();
            await _context.SaveChangesAsync();
        }
    }

    public async Task<UserProfileDTO> GetUserProfileAsync(string userLogin)
    {
        var user = await _context.Users
            .Include(u => u.Preferences)
            .FirstOrDefaultAsync(u => 
                (u.Email == userLogin || u.Username == userLogin) && 
                u.IsActive);

        if (user == null)
        {
            throw new InvalidOperationException("Пользователь не найден");
        }

        var stats = await GetUserStatsAsync(userLogin);

        return new UserProfileDTO(
            user.Id,
            user.Username,
            user.Email,
            user.Age,
            user.Weight,
            user.Height,
            user.Gender,
            user.Goal,
            user.ActivityLevel,
            user.BasalMetabolicRate,
            user.DailyCalorieTarget,
            user.Notes,
            user.CreatedAt,
            user.LastLoginAt,
            stats
        );
    }

    public async Task<UserProfileDTO> UpdateUserProfileAsync(string userLogin, UpdateUserProfileDTO updateData)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => 
                (u.Email == userLogin || u.Username == userLogin) && 
                u.IsActive);

        if (user == null)
        {
            throw new InvalidOperationException("Пользователь не найден");
        }

        // Проверка уникальности username и email при обновлении
        if (!string.IsNullOrEmpty(updateData.Username) && updateData.Username != user.Username)
        {
            var existingUser = await _context.Users
                .AnyAsync(u => u.Username == updateData.Username && u.Id != user.Id);
            if (existingUser)
            {
                throw new InvalidOperationException("Пользователь с таким именем уже существует");
            }
            user.Username = updateData.Username;
        }

        if (!string.IsNullOrEmpty(updateData.Email) && updateData.Email != user.Email)
        {
            var existingUser = await _context.Users
                .AnyAsync(u => u.Email == updateData.Email && u.Id != user.Id);
            if (existingUser)
            {
                throw new InvalidOperationException("Пользователь с таким email уже существует");
            }
            user.Email = updateData.Email;
            user.EmailVerified = false; // Требуется повторная верификация
        }

        // Обновление физических параметров
        if (updateData.Age.HasValue) user.Age = updateData.Age.Value;
        if (updateData.Weight.HasValue) user.Weight = updateData.Weight.Value;
        if (updateData.Height.HasValue) user.Height = updateData.Height.Value;
        if (!string.IsNullOrEmpty(updateData.Gender)) user.Gender = updateData.Gender;

        // Обновление целей и активности
        if (!string.IsNullOrEmpty(updateData.Goal)) user.Goal = updateData.Goal;
        if (!string.IsNullOrEmpty(updateData.ActivityLevel)) user.ActivityLevel = updateData.ActivityLevel;

        // Обновление заметок
        if (updateData.Notes != null) user.Notes = updateData.Notes;

        // Пересчет BMR и целевых калорий
        if (updateData.BasalMetabolicRate > 0)
        {
            user.BasalMetabolicRate = updateData.BasalMetabolicRate;
        }
        
        if (updateData.DailyCalorieTarget > 0)
        {
            user.DailyCalorieTarget = updateData.DailyCalorieTarget;
        }

        user.UpdateProfile();
        await _context.SaveChangesAsync();

        return await GetUserProfileAsync(userLogin);
    }

    public async Task ChangePasswordAsync(string userLogin, string currentPassword, string newPassword)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => 
                (u.Email == userLogin || u.Username == userLogin) && 
                u.IsActive);

        if (user == null)
        {
            throw new InvalidOperationException("Пользователь не найден");
        }

        // Проверка текущего пароля
        if (!_passwordService.VerifyPassword(currentPassword, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Неверный текущий пароль");
        }

        // Установка нового пароля
        user.PasswordHash = _passwordService.HashPassword(newPassword);
        user.PasswordSalt = null;
        user.UpdateProfile();

        // Отзыв всех активных сессий для безопасности
        var activeSessions = await _context.UserSessions
            .Where(s => s.UserId == user.Id && !s.IsRevoked)
            .ToListAsync();

        foreach (var session in activeSessions)
        {
            session.Revoke();
        }

        await _context.SaveChangesAsync();
    }

    public async Task<TokenValidationResponseDTO> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["JWT:Secret"]!);

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["JWT:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["JWT:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var username = jwtToken.Claims.First(x => x.Type == ClaimTypes.Name).Value;
            var exp = jwtToken.Claims.First(x => x.Type == JwtRegisteredClaimNames.Exp).Value;
            var expiryDate = DateTimeOffset.FromUnixTimeSeconds(long.Parse(exp)).DateTime;

            // Проверка в базе данных
            var tokenId = jwtToken.Claims.FirstOrDefault(x => x.Type == "jti")?.Value;
            if (!string.IsNullOrEmpty(tokenId))
            {
                var session = await _context.UserSessions
                    .FirstOrDefaultAsync(s => s.TokenId == tokenId && !s.IsRevoked);
                
                if (session == null)
                {
                    return new TokenValidationResponseDTO(false, null, null, "Токен отозван");
                }

                session.UpdateActivity();
                await _context.SaveChangesAsync();
            }

            return new TokenValidationResponseDTO(true, username, expiryDate, "Токен действителен");
        }
        catch (Exception ex)
        {
            return new TokenValidationResponseDTO(false, null, null, ex.Message);
        }
    }

    public async Task<bool> UserExistsByEmailAsync(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email == email);
    }

    public async Task<bool> UserExistsByUsernameAsync(string username)
    {
        return await _context.Users.AnyAsync(u => u.Username == username);
    }

    public async Task<string> RequestPasswordResetAsync(string email)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

        if (user == null)
        {
            // Для безопасности не раскрываем, существует ли пользователь
            return "reset_token"; // В реальности генерируем случайный токен
        }

        var resetToken = GenerateSecureToken();
        user.SetPasswordResetToken(resetToken, TimeSpan.FromHours(1));
        
        await _context.SaveChangesAsync();

        // Здесь должна быть отправка email с токеном
        _logger.LogInformation("Запрос на сброс пароля для {Email}, токен: {Token}", email, resetToken);

        return resetToken;
    }

    public async Task ConfirmPasswordResetAsync(ResetPasswordConfirmDTO resetData)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => 
                u.Email == resetData.Email && 
                u.PasswordResetToken == resetData.Token &&
                u.IsActive);

        if (user == null || !user.IsPasswordResetTokenValid())
        {
            throw new InvalidOperationException("Недействительный или истекший токен сброса");
        }

        var (hash, salt) = HashPassword(resetData.NewPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        user.ClearPasswordResetToken();

        // Отзыв всех активных сессий
        var activeSessions = await _context.UserSessions
            .Where(s => s.UserId == user.Id && !s.IsRevoked)
            .ToListAsync();

        foreach (var session in activeSessions)
        {
            session.Revoke();
        }

        await _context.SaveChangesAsync();
    }

    public async Task<UserStatsDTO> GetUserStatsAsync(string userLogin)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => 
                (u.Email == userLogin || u.Username == userLogin) && 
                u.IsActive);

        if (user == null)
        {
            throw new InvalidOperationException("Пользователь не найден");
        }

        var totalDays = (DateTime.UtcNow.Date - user.CreatedAt.Date).Days + 1;
        
        var foodEntries = await _context.FoodEntries
            .Where(f => f.UserId == user.Id)
            .GroupBy(f => f.EntryDate)
            .Select(g => new { Date = g.Key, TotalCalories = g.Sum(f => f.Calories) })
            .ToListAsync();

        var daysWithEntries = foodEntries.Count;
        var averageCalories = foodEntries.Any() ? foodEntries.Average(f => f.TotalCalories) : 0;
        var lastEntryDate = foodEntries.Any() ? 
            foodEntries.Max(f => f.Date).ToDateTime(TimeOnly.MinValue) : (DateTime?)null;

        return new UserStatsDTO(
            totalDays,
            daysWithEntries,
            (decimal)averageCalories,
            lastEntryDate
        );
    }

    public async Task UpdateLastLoginAsync(string userLogin)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => 
                (u.Email == userLogin || u.Username == userLogin) && 
                u.IsActive);

        if (user != null)
        {
            user.UpdateLastLogin();
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeactivateAccountAsync(string userLogin, string password)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => 
                (u.Email == userLogin || u.Username == userLogin) && 
                u.IsActive);

        if (user == null)
        {
            throw new InvalidOperationException("Пользователь не найден");
        }

        if (!VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
        {
            throw new UnauthorizedAccessException("Неверный пароль");
        }

        user.Deactivate();

        // Отзыв всех активных сессий
        var activeSessions = await _context.UserSessions
            .Where(s => s.UserId == user.Id && !s.IsRevoked)
            .ToListAsync();

        foreach (var session in activeSessions)
        {
            session.Revoke();
        }

        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<UserSessionDTO>> GetActiveSessionsAsync(string userLogin)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => 
                (u.Email == userLogin || u.Username == userLogin) && 
                u.IsActive);

        if (user == null)
        {
            throw new InvalidOperationException("Пользователь не найден");
        }

        var sessions = await _context.UserSessions
            .Where(s => s.UserId == user.Id && s.IsActive)
            .OrderByDescending(s => s.LastActivity)
            .ToListAsync();

        return sessions.Select(s => new UserSessionDTO(
            s.TokenId,
            s.DeviceInfo ?? "Неизвестное устройство",
            s.IpAddress ?? "Неизвестный IP",
            s.CreatedAt,
            s.LastActivity,
            false // Определение текущей сессии требует дополнительной логики
        ));
    }

    public async Task LogoutAllOtherSessionsAsync(string userLogin, string currentToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => 
                (u.Email == userLogin || u.Username == userLogin) && 
                u.IsActive);

        if (user == null)
        {
            throw new InvalidOperationException("Пользователь не найден");
        }

        var currentTokenId = ExtractTokenId(currentToken);
        
        var otherSessions = await _context.UserSessions
            .Where(s => s.UserId == user.Id && 
                       !s.IsRevoked && 
                       s.TokenId != currentTokenId)
            .ToListAsync();

        foreach (var session in otherSessions)
        {
            session.Revoke();
        }

        await _context.SaveChangesAsync();
    }

    #region Private Methods

    private async Task<(string Token, DateTime ExpiresAt)> GenerateJwtTokenAsync(Domain.Entities.Identity.User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["JWT:Secret"]!);
        var expiresAt = DateTime.UtcNow.AddDays(7); // Токен действует 7 дней
        var tokenId = Guid.NewGuid().ToString();

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, tokenId),
                new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            }),
            Expires = expiresAt,
            Issuer = _configuration["JWT:Issuer"],
            Audience = _configuration["JWT:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        // Сохранение сессии в базе данных
        var session = new UserSession
        {
            UserId = user.Id,
            TokenId = tokenId,
            ExpiresAt = expiresAt,
            DeviceInfo = "Web Browser", // Можно получать из User-Agent
            IpAddress = "0.0.0.0" // Можно получать из HttpContext
        };

        _context.UserSessions.Add(session);
        await _context.SaveChangesAsync();

        return (tokenString, expiresAt);
    }

    private static (string Hash, string Salt) HashPassword(string password)
    {
        using var rng = RandomNumberGenerator.Create();
        var saltBytes = new byte[32];
        rng.GetBytes(saltBytes);
        var salt = Convert.ToBase64String(saltBytes);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 10000, HashAlgorithmName.SHA256);
        var hash = Convert.ToBase64String(pbkdf2.GetBytes(32));

        return (hash, salt);
    }

    private static bool VerifyPassword(string password, string hash, string? salt)
    {
        if (string.IsNullOrEmpty(salt)) return false;

        var saltBytes = Convert.FromBase64String(salt);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 10000, HashAlgorithmName.SHA256);
        var testHash = Convert.ToBase64String(pbkdf2.GetBytes(32));

        return testHash == hash;
    }

    private static string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var tokenBytes = new byte[32];
        rng.GetBytes(tokenBytes);
        return Convert.ToBase64String(tokenBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    private static string? ExtractTokenId(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadJwtToken(token);
            return jsonToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti)?.Value;
        }
        catch
        {
            return null;
        }
    }

    #endregion
}