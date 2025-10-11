using System.Reflection;
using System.Text;
using System.Text.Json;
using FatSecret.DAL.Context;
using FatSecret.Domain.Models.DTO;
using FatSecret.Domain.Models.DTO.User;
using FatSecret.Service.Authentication;
using FatSecret.Service.Interfaces.Authentication;
using FatSecret.Service.User;
using FatSecret.Validation;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace FatSecret.Configuration;

/// <summary>
/// Расширения для конфигурации сервисов
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрация всех сервисов приложения
    /// </summary>
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // База данных
        services.AddDatabaseServices(configuration);
        
        // Аутентификация и авторизация
        services.AddAuthenticationServices(configuration);
        
        // Бизнес-логика
        services.AddBusinessServices();
        
        // Валидация
        services.AddValidationServices();
        
        // Фоновые сервисы
        services.AddBackgroundServices();
        
        // Health Checks
        services.AddHealthCheckServices();

        return services;
    }

    /// <summary>
    /// Конфигурация сервисов базы данных
    /// </summary>
    public static IServiceCollection AddDatabaseServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Основной DbContext
        services.AddDbContext<FatSecretDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            
            options.UseNpgsql(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
                
                sqlOptions.CommandTimeout(30);
            });

            // Настройки для разработки
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        // Repository pattern (опционально)
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    /// <summary>
    /// Конфигурация аутентификации и авторизации
    /// </summary>
    public static IServiceCollection AddAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // JWT конфигурация
        var jwtSettings = configuration.GetSection("JWT");
        var secretKey = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret не настроен");
        var key = Encoding.ASCII.GetBytes(secretKey);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false; // В production должно быть true
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSettings["Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                RequireExpirationTime = true
            };

            // Дополнительная проверка токена в базе данных
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
                    var token = context.Request.Headers["Authorization"]
                        .FirstOrDefault()?.Split(" ").Last();

                    if (!string.IsNullOrEmpty(token))
                    {
                        var validationResult = await authService.ValidateTokenAsync(token);
                        if (!validationResult.IsValid)
                        {
                            context.Fail("Токен недействителен");
                        }
                    }
                }
            };
        });

        services.AddAuthorization(options =>
        {
            // Можно добавить кастомные политики
            options.AddPolicy("RequireActiveUser", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireClaim("user_active", "true"));
        });

        return services;
    }

    /// <summary>
    /// Конфигурация бизнес-сервисов
    /// </summary>
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        // Сервисы аутентификации
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<CreateUser>();

        // Другие бизнес-сервисы можно добавить здесь
        // services.AddScoped<IFoodService, FoodService>();
        // services.AddScoped<IDiaryService, DiaryService>();

        return services;
    }

    /// <summary>
    /// Конфигурация валидации
    /// </summary>
    public static IServiceCollection AddValidationServices(this IServiceCollection services)
    {
        // FluentValidation
        services.AddValidatorsFromAssemblyContaining<CreateUserRequestValidator>();
        
        // Регистрация конкретных валидаторов
        services.AddScoped<IValidator<CreateUserRequestsDTO>, CreateUserRequestValidator>();
        services.AddScoped<IValidator<LoginRequestDTO>, LoginRequestValidator>();
        services.AddScoped<IValidator<UpdateUserProfileDTO>, UpdateUserProfileValidator>();
        services.AddScoped<IValidator<ChangePasswordDTO>, ChangePasswordValidator>();
        services.AddScoped<IValidator<ResetPasswordRequestDTO>, ResetPasswordRequestValidator>();
        services.AddScoped<IValidator<ResetPasswordConfirmDTO>, ResetPasswordConfirmValidator>();

        return services;
    }

    /// <summary>
    /// Конфигурация фоновых сервисов
    /// </summary>
    public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
    {
        services.AddHostedService<DatabaseCleanupService>();
        
        // Другие фоновые сервисы
        // services.AddHostedService<EmailNotificationService>();

        return services;
    }

    /// <summary>
    /// Конфигурация Health Checks
    /// </summary>
    public static IServiceCollection AddHealthCheckServices(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database")
            .AddDbContextCheck<FatSecretDbContext>("efcore");

        return services;
    }

    /// <summary>
    /// Конфигурация CORS
    /// </summary>
    public static IServiceCollection AddCorsServices(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("VueApp", policy =>
            {
                policy.WithOrigins(
                    "http://localhost:3000",
                    "http://localhost:8080", 
                    "http://127.0.0.1:5500",
                    "https://localhost:5001")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });

            options.AddPolicy("Production", policy =>
            {
                policy.WithOrigins("https://yourdomain.com")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        return services;
    }

    /// <summary>
    /// Конфигурация API документации
    /// </summary>
    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { 
                Title = "FatSecret API", 
                Version = "v1",
                Description = "API для дневника питания FatSecret"
            });

            // JWT авторизация в Swagger
            c.AddSecurityDefinition("Bearer", new()
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            c.AddSecurityRequirement(new()
            {
                {
                    new()
                    {
                        Reference = new() { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });

            // Подключение XML комментариев
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }
        });

        return services;
    }
}

/// <summary>
/// Расширения для конфигурации middleware
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Конфигурация pipeline для разработки
    /// </summary>
    public static WebApplication ConfigureDevelopmentPipeline(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "FatSecret API V1");
            c.RoutePrefix = "swagger";
        });

        app.UseDeveloperExceptionPage();

        return app;
    }

    /// <summary>
    /// Конфигурация pipeline для production
    /// </summary>
    public static WebApplication ConfigureProductionPipeline(this WebApplication app)
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
        app.UseHttpsRedirection();

        return app;
    }

    /// <summary>
    /// Общая конфигурация pipeline
    /// </summary>
    public static WebApplication ConfigureCommonPipeline(this WebApplication app)
    {
        // CORS
        var environment = app.Environment.EnvironmentName;
        var corsPolicy = environment == "Production" ? "Production" : "VueApp";
        app.UseCors(corsPolicy);

        // Статические файлы (если нужны)
        app.UseStaticFiles();

        // Аутентификация и авторизация
        app.UseAuthentication();
        app.UseAuthorization();

        // Health checks
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var result = JsonSerializer.Serialize(new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description,
                        data = e.Value.Data
                    })
                });
                await context.Response.WriteAsync(result);
            }
        });

        // Контроллеры
        app.MapControllers();

        return app;
    }

    /// <summary>
    /// Инициализация базы данных при запуске
    /// </summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.InitializeDatabaseAsync();
    }
}

/// <summary>
/// Пример конфигурации в Program.cs
/// </summary>
public static class ProgramExample
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Регистрация всех сервисов
        builder.Services.AddApplicationServices(builder.Configuration);
        builder.Services.AddCorsServices();
        builder.Services.AddApiDocumentation();
        builder.Services.AddControllers();

        var app = builder.Build();

        // Конфигурация pipeline
        if (app.Environment.IsDevelopment())
        {
            app.ConfigureDevelopmentPipeline();
        }
        else
        {
            app.ConfigureProductionPipeline();
        }

        app.ConfigureCommonPipeline();

        // Инициализация базы данных
        await app.InitializeDatabaseAsync();

        await app.RunAsync();
    }
}