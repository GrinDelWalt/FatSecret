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
        optionsBuilder.UseNpgsql("Server=(localdb)\\mssqllocaldb;Database=FatSecretDB;Trusted_Connection=true;MultipleActiveResultSets=true");
        
        return new FatSecretDbContext(optionsBuilder.Options);
    }
}