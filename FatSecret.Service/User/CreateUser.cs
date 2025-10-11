using System;
using System.Threading.Tasks;
using FatSecret.DAL.Interfaces;
using FatSecret.Domain.Models.DTO;
using FatSecret.Domain.Models.DTO.User;
using FatSecret.Service.Interfaces.Service;
using FatSecret.Service.Interfaces.Password;
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
            .Where(x => x.Email == request.Email ||  x.Username == request.Username)
            .FirstOrDefaultAsync();

        if (existingUser != null)
        {
            _logger.LogWarning("Attempt to create user with existing login {Email} or email {Email}", 
                request.Email, request.Email);
            throw new InvalidOperationException("Пользователь с таким логином или email уже существует");
        }

        // Хеширование пароля
        string hashedPassword = _passwordService.HashPassword(request.Password);

        // Создание нового пользователя
        var newUser = new Domain.Entities.Identity.User()
        {
            Email = request.Email.ToLowerInvariant(),
            Username = request.Username,
            Age = request.Age,
            Weight = request.Weight,
            Height = request.Height,
            Gender = request.Gender,
            Goal = request.Goal,
            ActivityLevel = request.ActivityLevel,
            BasalMetabolicRate = request.BasalMetabolicRate,
            DailyCalorieTarget = request.DailyCalorieTarget,
            Notes = request.Notes,
            PasswordHash = hashedPassword, // Содержит соль+хеш
            PasswordSalt = null // Не используется с PasswordService
        };

        _userRepository.Add(newUser);
        await _userRepository.SaveChangesAsync();

        _logger.LogInformation("User {Username} created successfully", request.Username);

        return new CreateUserResponseDTO(
            newUser.Id,
            newUser.Username,
            newUser.Email,
            newUser.BasalMetabolicRate,
            newUser.DailyCalorieTarget,
            newUser.CreatedAt
        );
    }
}