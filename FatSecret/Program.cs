using FatSecret.DAL;
using FatSecret.DAL.Context;
using FatSecret.Initialization;
using FatSecret.Service.Authentication;
using FatSecret.Service.Interfaces.Authentication;
using FatSecret.Service.Interfaces.Password;
using FatSecret.Service.Interfaces.Repository;
using FatSecret.Service.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;

namespace FatSecret;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Настройка Serilog
        builder.Host.UseSerilog((context, configuration) =>
        {
            configuration.ReadFrom.Configuration(context.Configuration);
        });

        // Add services to the container.
        builder.Services.AddAuthorization();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        builder.Services.AddControllers();

        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo 
            { 
                Title = "FatSecret API", 
                Version = "v1",
                Description = "API для управления питанием и калориями"
            });

            // Добавляем поддержку JWT в Swagger
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement()
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        },
                        Scheme = "oauth2",
                        Name = "Bearer",
                        In = ParameterLocation.Header,
                    },
                    new List<string>()
                }
            });
        });

        // Database configuration
        builder.Services.AddDbContext<FatSecretDbContext>(options =>
        {
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            options.UseNpgsql(connectionString);
        });

        // Repository registration
        builder.Services.AddScoped(typeof(IEntityRepository<>), typeof(EntityRepository<>));

        // Service registration
        builder.Services.AddScoped<IPasswordService, PasswordService>();
        builder.Services.AddScoped<IJwtService, JwtService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<CreateUser>();

        // Database initialization
        builder.Services.AddScoped<IDbInitializer, DatabaseInitializer>();
        builder.Services.AddHostedService<DatabaseInitializer>();

        // JWT Authentication
        builder.Services.AddJWTAuthentication(builder.Configuration);
        
        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwagger();
            app.UseSwaggerUI(c => 
            { 
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "FatSecret API v1");
                c.RoutePrefix = string.Empty; // Swagger UI на корневом пути
            });
        }

        app.UseSerilogRequestLogging();

        app.UseHttpsRedirection();

        app.UseAuthentication(); // Важно: добавляем ПЕРЕД Authorization
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}