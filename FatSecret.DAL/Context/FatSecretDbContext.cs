using FatSecret.DAL.Mappings;
using FatSecret.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FatSecret.DAL.Context;

public class FatSecretDbContext : DbContext
{
    private readonly ILogger _logger;

    public FatSecretDbContext(DbContextOptions<FatSecretDbContext> options) : base(options)
    {
    }
    
    public DbSet<User> Users { get; set; }
    public DbSet<UserToken> UserTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserMap());
        modelBuilder.ApplyConfiguration(new UserTokenMap());
    }
}