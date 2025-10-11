using FatSecret.DAL;
using FatSecret.DAL.Context;
using Microsoft.EntityFrameworkCore;

namespace FatSecret.Initialization;

public class DatabaseInitializer : IDbInitializer, IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public DatabaseInitializer(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public async Task InitializeAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FatSecretDbContext>();
        await context.Database.MigrateAsync();
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FatSecretDbContext>();

        try
        {
            dbContext.Database.Migrate();
            Console.WriteLine("Database migrated");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при миграции базы данных: {ex.Message}");
            throw; // Перебрасываем ошибку, чтобы приложение не продолжало работу
        }
        
        Console.WriteLine("Done");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
    
}