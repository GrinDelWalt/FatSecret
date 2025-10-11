using FatSecret.Configuration;
using FatSecret.DAL;
using FatSecret.DAL.Context;
using FatSecret.DAL.Interfaces;
using FatSecret.Initialization;
using FatSecret.Service.Authentication;
using FatSecret.Service.Interfaces.Authentication;
using FatSecret.Service.Interfaces.Password;
using FatSecret.Service.User;
using FatSecret.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;

namespace FatSecret;

public class Program
{
    public static void Main(string[] args)
    {
        MainAsync(args).GetAwaiter().GetResult();
    }

    public static async Task MainAsync(string[] args)
    {
        try
        {
            var builder = WebApplication.CreateBuilder(args);

            // Настройка Serilog
            builder.Host.UseSerilog((context, configuration) =>
            {
                configuration.ReadFrom.Configuration(context.Configuration);
            });

            builder.Services.AddControllers();
            builder.Services.AddAuthorization();
            builder.Services.AddOpenApi();

            // DATABASE CONFIGURATION ПЕРВЫМ (до регистрации сервисов которые его используют)
            builder.Services.AddDbContext<FatSecretDbContext>(options =>
            {
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException("Connection string не настроен");
                }
                
                options.UseNpgsql(connectionString);
                
                // Настройки для разработки
                if (builder.Environment.IsDevelopment())
                {
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                }
            });

            // JWT Authentication ПЕРЕД сервисами которые его используют
            builder.Services.AddJWTAuthentication(builder.Configuration);

            // REPOSITORY registration (после DbContext)
            builder.Services.AddScoped(typeof(IEntityRepository<>), typeof(EntityRepository<>));

            // SERVICE registration (после всех зависимостей)
            builder.Services.AddScoped<IPasswordService, PasswordService>();
            builder.Services.AddScoped<IJwtService, JwtService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<CreateUser>();

            // Database initialization
            builder.Services.AddScoped<IDbInitializer, DatabaseInitializer>();
            builder.Services.AddScoped<FatSecret.Filters.ValidationFilterAttribute>();

            // VALIDATION (в самом конце)
            builder.Services.AddCustomValidation();

            // CORS configuration
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("VueApp", policy =>
                {
                    policy.WithOrigins("http://localhost:3000", 
                            "http://127.0.0.1:5500",
                            "http://localhost:5500",
                            "http://localhost:7000",
                            "null"  ) 
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            // SWAGGER configuration
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo 
                { 
                    Title = "FatSecret API", 
                    Version = "v1",
                    Description = "API для управления питанием и калориями"
                });

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

            var app = builder.Build();

            
            // Логирование
            app.UseSerilogRequestLogging();
            
            // Обработка ошибок валидации
            app.UseValidationExceptionHandling();

            // Development tools
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI(c => 
                { 
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "FatSecret API v1");
                    c.RoutePrefix = string.Empty;
                });
            }

            app.UseHttpsRedirection();

            // CORS (до аутентификации)
            app.UseCors("VueApp");

            // Аутентификация и авторизация
            app.UseAuthentication(); 
            app.UseAuthorization();

            // Маршрутизация
            app.MapControllers();

            // Инициализация базы данных (ASYNC properly handled)
            // ????????????? ???? ?????? ????? ????????
            try
            {
                using (var scope = app.Services.CreateScope())
                {
                    var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
                    await dbInitializer.InitializeAsync();
                    Console.WriteLine("???? ?????? ??????? ????????????????");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"?????? ????????????? ???? ??????: {ex.Message}");
            }
            
            Console.WriteLine("Host start");
            app.Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Критическая ошибка при запуске: {ex.Message}");
            Console.WriteLine($"Детали: {ex}");
            throw;
        }
    }
}