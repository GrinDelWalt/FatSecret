namespace FatSecret.DAL.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

public class FatSecretDbContextFactory : IDesignTimeDbContextFactory<FatSecretDbContext>
{
    public FatSecretDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<FatSecretDbContext>();
        
        // Строка подключения для миграций
        optionsBuilder.UseNpgsql( "Host=localhost;Port=5432;Database=FatSecretDb;Username=FatAdmin;Password=FatAdmin");
        
        return new FatSecretDbContext(optionsBuilder.Options);
    }
}

