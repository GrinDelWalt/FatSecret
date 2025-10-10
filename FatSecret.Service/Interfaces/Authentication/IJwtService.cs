namespace FatSecret.Service.Interfaces.Authentication;

public interface IJwtService
{
    Task<string> GenerateTokenAsync(string login);
    Task<bool> ValidateTokenAsync(string token);
    Task RevokeTokenAsync(string token);
}