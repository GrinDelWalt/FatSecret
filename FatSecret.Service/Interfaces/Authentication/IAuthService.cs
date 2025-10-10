namespace FatSecret.Service.Interfaces.Authentication;

public interface IAuthService
{
    Task<string> LoginAsync(string login, string password);
    Task LogoutAsync(string token);
}