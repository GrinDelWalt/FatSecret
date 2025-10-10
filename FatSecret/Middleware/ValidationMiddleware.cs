
namespace FatSecret.Middleware;

public class ValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceProvider _serviceProvider;

    public ValidationMiddleware(RequestDelegate next, IServiceProvider serviceProvider)
    {
        _next = next;
        _serviceProvider = serviceProvider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Продолжаем выполнение pipeline
        await _next(context);
    }
}

// Расширение для удобной регистрации middleware
public static class ValidationMiddlewareExtensions
{
    public static IApplicationBuilder UseValidation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ValidationMiddleware>();
    }
}