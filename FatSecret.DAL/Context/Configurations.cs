using System.Linq.Expressions;
using FatSecret.Domain.Entities.Identity;
using FatSecret.Domain.Models.DTO.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FatSecret.DAL.Context;

#region Value Converters

/// <summary>
/// Конвертер для DateOnly в старых версиях EF Core
/// </summary>
public class DateOnlyConverter : ValueConverter<DateOnly, DateTime>
{
    public DateOnlyConverter() : base(
        dateOnly => dateOnly.ToDateTime(TimeOnly.MinValue),
        dateTime => DateOnly.FromDateTime(dateTime))
    {
    }
}

/// <summary>
/// Конвертер для TimeOnly
/// </summary>
public class TimeOnlyConverter : ValueConverter<TimeOnly, TimeSpan>
{
    public TimeOnlyConverter() : base(
        timeOnly => timeOnly.ToTimeSpan(),
        timeSpan => TimeOnly.FromTimeSpan(timeSpan))
    {
    }
}

#endregion

#region Entity Type Configurations

/// <summary>
/// Базовая конфигурация для всех сущностей
/// </summary>
public abstract class BaseEntityConfiguration<T> : IEntityTypeConfiguration<T> where T : class
{
    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        // Общие настройки для всех сущностей
        ConfigureCommonProperties(builder);
    }

    protected virtual void ConfigureCommonProperties(EntityTypeBuilder<T> builder)
    {
        // Настройки, общие для всех сущностей
        // Например, автоматическое обновление временных меток
    }
}

/// <summary>
/// Отдельная конфигурация для User
/// </summary>
public class UserConfiguration : BaseEntityConfiguration<User>
{
    public override void Configure(EntityTypeBuilder<User> builder)
    {
        base.Configure(builder);

        builder.ToTable("Users");

        // Первичный ключ
        builder.HasKey(u => u.Id);

        // Индексы
        builder.HasIndex(u => u.Username).IsUnique();
        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.EmailVerificationToken);
        builder.HasIndex(u => u.PasswordResetToken);

        // Свойства
        builder.Property(u => u.Username)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(u => u.Email)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(u => u.Weight)
            .HasColumnType("decimal(5,2)");

        builder.Property(u => u.Goal)
            .HasMaxLength(20)
            .HasDefaultValue("maintain");

        builder.Property(u => u.ActivityLevel)
            .HasMaxLength(20)
            .HasDefaultValue("moderate");

        builder.Property(u => u.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(u => u.IsActive)
            .HasDefaultValue(true);

        // Ограничения
        builder.HasCheckConstraint("CK_Users_Age", "[age] >= 0 AND [age] <= 150");
        builder.HasCheckConstraint("CK_Users_Weight", "[weight] >= 0 AND [weight] <= 1000");
        builder.HasCheckConstraint("CK_Users_Height", "[height] >= 0 AND [height] <= 300");

        // Связи
        builder.HasMany(u => u.Sessions)
            .WithOne(s => s.User)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(u => u.Preferences)
            .WithOne(p => p.User)
            .HasForeignKey<UserPreferences>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

#endregion

#region Database Extensions

/// <summary>
/// Расширения для работы с базой данных
/// </summary>
public static class DatabaseExtensions
{
    /// <summary>
    /// Применение всех миграций и seed данных
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FatSecretDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<FatSecretDbContext>>();

        try
        {
            logger.LogInformation("Начало инициализации базы данных...");

            // Применение миграций
            await context.Database.MigrateAsync();
            logger.LogInformation("Миграции применены успешно");

            // Seed данные
            await SeedDataAsync(context, logger);
            logger.LogInformation("База данных инициализирована успешно");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при инициализации базы данных");
            throw;
        }
    }

    /// <summary>
    /// Заполнение базы начальными данными
    /// </summary>
    private static async Task SeedDataAsync(FatSecretDbContext context, ILogger logger)
    {
        // Проверяем, есть ли уже данные
        if (await context.Users.AnyAsync())
        {
            logger.LogInformation("База данных уже содержит данные, пропускаем seed");
            return;
        }

        logger.LogInformation("Заполнение базы начальными данными...");

        // Можно добавить тестовых пользователей, справочники и т.д.
        // Пример:
        /*
        var adminUser = new User
        {
            Username = "admin",
            Email = "admin@fatsecret.local",
            PasswordHash = HashPassword("admin123"),
            IsActive = true,
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            Goal = "maintain",
            ActivityLevel = "moderate"
        };

        context.Users.Add(adminUser);
        await context.SaveChangesAsync();
        */

        logger.LogInformation("Начальные данные добавлены");
    }

    /// <summary>
    /// Очистка устаревших сессий
    /// </summary>
    public static async Task CleanupExpiredSessionsAsync(this FatSecretDbContext context)
    {
        var expiredSessions = await context.UserSessions
            .Where(s => s.ExpiresAt < DateTime.UtcNow || s.IsRevoked)
            .ToListAsync();

        if (expiredSessions.Any())
        {
            context.UserSessions.RemoveRange(expiredSessions);
            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Получение статистики базы данных
    /// </summary>
    public static async Task<DatabaseStatsDTO> GetDatabaseStatsAsync(this FatSecretDbContext context)
    {
        var userCount = await context.Users.CountAsync(u => u.IsActive);
        var sessionCount = await context.UserSessions.CountAsync(s => !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow);
        var foodEntryCount = await context.FoodEntries.CountAsync();
        var weightRecordCount = await context.WeightRecords.CountAsync();

        return new DatabaseStatsDTO(
            userCount,
            sessionCount,
            foodEntryCount,
            weightRecordCount,
            DateTime.UtcNow
        );
    }
}

/// <summary>
/// DTO для статистики базы данных
/// </summary>
public record DatabaseStatsDTO(
    int ActiveUsers,
    int ActiveSessions,
    int TotalFoodEntries,
    int TotalWeightRecords,
    DateTime GeneratedAt
);

#endregion

#region Background Services

/// <summary>
/// Фоновый сервис для очистки устаревших данных
/// </summary>
public class DatabaseCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseCleanupService> _logger;
    private readonly TimeSpan _period = TimeSpan.FromHours(6); // Запуск каждые 6 часов

    public DatabaseCleanupService(
        IServiceProvider serviceProvider,
        ILogger<DatabaseCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DoWorkAsync();
                await Task.Delay(_period, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Нормальная остановка сервиса
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка в DatabaseCleanupService");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Пауза перед повтором
            }
        }
    }

    private async Task DoWorkAsync()
    {
        _logger.LogInformation("Запуск очистки базы данных...");

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FatSecretDbContext>();

        // Очистка устаревших сессий
        await context.CleanupExpiredSessionsAsync();

        // Очистка устаревших токенов сброса пароля
        var expiredResetTokens = await context.Users
            .Where(u => u.PasswordResetExpires.HasValue && u.PasswordResetExpires < DateTime.UtcNow)
            .ToListAsync();

        foreach (var user in expiredResetTokens)
        {
            user.ClearPasswordResetToken();
        }

        if (expiredResetTokens.Any())
        {
            await context.SaveChangesAsync();
        }

        _logger.LogInformation("Очистка базы данных завершена. Очищено сессий: {SessionCount}, токенов: {TokenCount}",
            0, expiredResetTokens.Count); // Количество очищенных сессий можно добавить в метод CleanupExpiredSessionsAsync
    }
}

#endregion

#region Repository Pattern (Опционально)

/// <summary>
/// Базовый интерфейс репозитория
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T?> SingleOrDefaultAsync(Expression<Func<T, bool>> predicate);
    Task AddAsync(T entity);
    Task AddRangeAsync(IEnumerable<T> entities);
    void Update(T entity);
    void Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);
    Task<int> SaveChangesAsync();
}

/// <summary>
/// Базовая реализация репозитория
/// </summary>
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly FatSecretDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(FatSecretDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    public virtual async Task<T?> SingleOrDefaultAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.SingleOrDefaultAsync(predicate);
    }

    public virtual async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
    }

    public virtual async Task AddRangeAsync(IEnumerable<T> entities)
    {
        await _dbSet.AddRangeAsync(entities);
    }

    public virtual void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    public virtual void Remove(T entity)
    {
        _dbSet.Remove(entity);
    }

    public virtual void RemoveRange(IEnumerable<T> entities)
    {
        _dbSet.RemoveRange(entities);
    }

    public virtual async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}

/// <summary>
/// Специализированный репозиторий для пользователей
/// </summary>
public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByUsernameOrEmailAsync(string login);
    Task<bool> IsUsernameUniqueAsync(string username, int? excludeUserId = null);
    Task<bool> IsEmailUniqueAsync(string email, int? excludeUserId = null);
    Task<IEnumerable<User>> GetActiveUsersAsync();
    Task<UserStatsDTO> GetUserStatsAsync(int userId);
}

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(FatSecretDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _dbSet
            .Include(u => u.Preferences)
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet
            .Include(u => u.Preferences)
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
    }

    public async Task<User?> GetByUsernameOrEmailAsync(string login)
    {
        return await _dbSet
            .Include(u => u.Preferences)
            .FirstOrDefaultAsync(u => (u.Username == login || u.Email == login) && u.IsActive);
    }

    public async Task<bool> IsUsernameUniqueAsync(string username, int? excludeUserId = null)
    {
        var query = _dbSet.Where(u => u.Username == username);
        
        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.Id != excludeUserId.Value);
        }

        return !await query.AnyAsync();
    }

    public async Task<bool> IsEmailUniqueAsync(string email, int? excludeUserId = null)
    {
        var query = _dbSet.Where(u => u.Email == email);
        
        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.Id != excludeUserId.Value);
        }

        return !await query.AnyAsync();
    }

    public async Task<IEnumerable<User>> GetActiveUsersAsync()
    {
        return await _dbSet
            .Where(u => u.IsActive)
            .OrderByDescending(u => u.LastLoginAt)
            .ToListAsync();
    }

    public async Task<UserStatsDTO> GetUserStatsAsync(int userId)
    {
        var user = await _dbSet.FindAsync(userId);
        if (user == null) throw new ArgumentException("Пользователь не найден");

        var totalDays = (DateTime.UtcNow.Date - user.CreatedAt.Date).Days + 1;
        
        var foodEntries = await _context.FoodEntries
            .Where(f => f.UserId == userId)
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
}

#endregion

#region Unit of Work Pattern

/// <summary>
/// Интерфейс Unit of Work
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IRepository<UserSession> UserSessions { get; }
    IRepository<UserPreferences> UserPreferences { get; }
    IRepository<WeightRecord> WeightRecords { get; }
    IRepository<FoodEntry> FoodEntries { get; }
    
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}

/// <summary>
/// Реализация Unit of Work
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly FatSecretDbContext _context;
    private IDbContextTransaction? _transaction;

    private IUserRepository? _userRepository;
    private IRepository<UserSession>? _userSessionRepository;
    private IRepository<UserPreferences>? _userPreferencesRepository;
    private IRepository<WeightRecord>? _weightRecordRepository;
    private IRepository<FoodEntry>? _foodEntryRepository;

    public UnitOfWork(FatSecretDbContext context)
    {
        _context = context;
    }

    public IUserRepository Users => 
        _userRepository ??= new UserRepository(_context);

    public IRepository<UserSession> UserSessions => 
        _userSessionRepository ??= new Repository<UserSession>(_context);

    public IRepository<UserPreferences> UserPreferences => 
        _userPreferencesRepository ??= new Repository<UserPreferences>(_context);

    public IRepository<WeightRecord> WeightRecords => 
        _weightRecordRepository ??= new Repository<WeightRecord>(_context);

    public IRepository<FoodEntry> FoodEntries => 
        _foodEntryRepository ??= new Repository<FoodEntry>(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}

#endregion

#region Database Health Checks

/// <summary>
/// Health Check для базы данных
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly FatSecretDbContext _context;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(FatSecretDbContext context, ILogger<DatabaseHealthCheck> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Простая проверка подключения
            await _context.Database.CanConnectAsync(cancellationToken);
            
            // Проверка количества пользователей
            var userCount = await _context.Users.CountAsync(cancellationToken);
            
            var data = new Dictionary<string, object>
            {
                ["users_count"] = userCount,
                ["database_provider"] = _context.Database.ProviderName ?? "Unknown"
            };

            return HealthCheckResult.Healthy("База данных доступна", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при проверке состояния базы данных");
            return HealthCheckResult.Unhealthy("База данных недоступна", ex);
        }
    }
}

#endregion