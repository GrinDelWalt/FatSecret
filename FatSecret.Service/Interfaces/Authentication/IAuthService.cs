using FatSecret.Domain.Models.DTO;
using FatSecret.Domain.Models.DTO.User;

namespace FatSecret.Service.Interfaces.Authentication;

/// <summary>
/// Интерфейс сервиса аутентификации и управления пользователями
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Аутентификация пользователя
    /// </summary>
    /// <param name="login">Email или имя пользователя</param>
    /// <param name="password">Пароль</param>
    /// <returns>Данные для входа с токеном</returns>
    Task<LoginResponseDTO> LoginAsync(string login, string password);

    /// <summary>
    /// Выход пользователя (отзыв токена)
    /// </summary>
    /// <param name="token">JWT токен</param>
    Task LogoutAsync(string token);

    /// <summary>
    /// Получение полного профиля пользователя
    /// </summary>
    /// <param name="userLogin">Логин пользователя</param>
    /// <returns>Профиль пользователя</returns>
    Task<UserProfileDTO> GetUserProfileAsync(string userLogin);

    /// <summary>
    /// Обновление профиля пользователя
    /// </summary>
    /// <param name="userLogin">Логин пользователя</param>
    /// <param name="updateData">Данные для обновления</param>
    /// <returns>Обновленный профиль</returns>
    Task<UserProfileDTO> UpdateUserProfileAsync(string userLogin, UpdateUserProfileDTO updateData);

    /// <summary>
    /// Смена пароля пользователя
    /// </summary>
    /// <param name="userLogin">Логин пользователя</param>
    /// <param name="currentPassword">Текущий пароль</param>
    /// <param name="newPassword">Новый пароль</param>
    Task ChangePasswordAsync(string userLogin, string currentPassword, string newPassword);

    /// <summary>
    /// Валидация JWT токена
    /// </summary>
    /// <param name="token">JWT токен</param>
    /// <returns>Результат валидации</returns>
    Task<TokenValidationResponseDTO> ValidateTokenAsync(string token);

    /// <summary>
    /// Проверка существования пользователя по email
    /// </summary>
    /// <param name="email">Email адрес</param>
    /// <returns>True если пользователь существует</returns>
    Task<bool> UserExistsByEmailAsync(string email);

    /// <summary>
    /// Проверка существования пользователя по имени
    /// </summary>
    /// <param name="username">Имя пользователя</param>
    /// <returns>True если пользователь существует</returns>
    Task<bool> UserExistsByUsernameAsync(string username);

    /// <summary>
    /// Запрос на сброс пароля
    /// </summary>
    /// <param name="email">Email адрес</param>
    /// <returns>Токен для сброса пароля</returns>
    Task<string> RequestPasswordResetAsync(string email);

    /// <summary>
    /// Подтверждение сброса пароля
    /// </summary>
    /// <param name="resetData">Данные для сброса</param>
    Task ConfirmPasswordResetAsync(ResetPasswordConfirmDTO resetData);

    /// <summary>
    /// Получение статистики пользователя
    /// </summary>
    /// <param name="userLogin">Логин пользователя</param>
    /// <returns>Статистика пользователя</returns>
    Task<UserStatsDTO> GetUserStatsAsync(string userLogin);

    /// <summary>
    /// Обновление времени последнего входа
    /// </summary>
    /// <param name="userLogin">Логин пользователя</param>
    Task UpdateLastLoginAsync(string userLogin);

    /// <summary>
    /// Деактивация аккаунта пользователя
    /// </summary>
    /// <param name="userLogin">Логин пользователя</param>
    /// <param name="password">Пароль для подтверждения</param>
    Task DeactivateAccountAsync(string userLogin, string password);

    /// <summary>
    /// Получение списка активных сессий пользователя
    /// </summary>
    /// <param name="userLogin">Логин пользователя</param>
    /// <returns>Список активных токенов</returns>
    Task<IEnumerable<UserSessionDTO>> GetActiveSessionsAsync(string userLogin);

    /// <summary>
    /// Завершение всех сессий пользователя кроме текущей
    /// </summary>
    /// <param name="userLogin">Логин пользователя</param>
    /// <param name="currentToken">Текущий токен (не будет отозван)</param>
    Task LogoutAllOtherSessionsAsync(string userLogin, string currentToken);
}

/// <summary>
/// DTO для информации о сессии пользователя
/// </summary>
public record UserSessionDTO(
    string TokenId,
    string DeviceInfo,
    string IpAddress,
    DateTime CreatedAt,
    DateTime LastActivity,
    bool IsCurrent
);