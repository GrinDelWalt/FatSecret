using FatSecret.Domain.Models.DTO;
using FatSecret.Service.Interfaces.Service;
using FatSecret.Service.Interfaces.Password;
using FatSecret.Service.Interfaces.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FatSecret.Service.User;

public class CreateUser : IService<CreateUserRequestsDTO, CreateUserResponseDTO>
{
    private readonly IEntityRepository<Domain.Entities.Identity.User> _userRepository;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<CreateUser> _logger;

    public CreateUser(
        IEntityRepository<Domain.Entities.Identity.User> userRepository,
        IPasswordService passwordService,
        ILogger<CreateUser> logger)
    {
        _userRepository = userRepository;
        _passwordService = passwordService;
        _logger = logger;
    }

    public async Task<CreateUserResponseDTO> Execute(CreateUserRequestsDTO request)
    {
        // Проверка на существование пользователя
        var existingUser = await _userRepository.All()
            .Where(x => x.Login == request.Login || x.Email == request.Email)
            .FirstOrDefaultAsync();

        if (existingUser != null)
        {
            _logger.LogWarning("Attempt to create user with existing login {Login} or email {Email}", 
                request.Login, request.Email);
            throw new InvalidOperationException("Пользователь с таким логином или email уже существует");
        }

        // Хеширование пароля
        string hashedPassword = _passwordService.HashPassword(request.Password);

        // Создание нового пользователя
        var newUser = new Domain.Entities.Identity.User
        {
            Email = request.Email.ToLowerInvariant(), // Нормализация email
            Login = request.Login,
            Password = hashedPassword, // Сохраняем хешированный пароль
            FirstName = request.FirstName,
            LastName = request.LastName,
        };

        _userRepository.Add(newUser);
        await _userRepository.SaveChangesAsync();

        _logger.LogInformation("User {Login} created successfully", request.Login);

        // Возвращаем данные без пароля
        return new CreateUserResponseDTO(
            newUser.Email,
            "*****", // Не возвращаем реальный пароль
            newUser.LastName,
            newUser.FirstName,
            newUser.Login
        );
    }
}