using FatSecret.Domain.Models.API;
using Microsoft.AspNetCore.Diagnostics;
using System.Net;
using System.Text.Json;

namespace FatSecret.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "An unhandled exception occurred");

        var response = new ApiResponse<object>
        {
            Success = false,
            Message = GetUserFriendlyMessage(exception),
            Data = null
        };

        var statusCode = GetStatusCode(exception);

        httpContext.Response.StatusCode = (int)statusCode;
        httpContext.Response.ContentType = "application/json";

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await httpContext.Response.WriteAsync(jsonResponse, cancellationToken);

        return true;
    }

    private static HttpStatusCode GetStatusCode(Exception exception) =>
        exception switch
        {
            FluentValidation.ValidationException => HttpStatusCode.BadRequest,
            ArgumentNullException => HttpStatusCode.BadRequest,
            ArgumentException => HttpStatusCode.BadRequest,
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            InvalidOperationException => HttpStatusCode.Conflict,
            KeyNotFoundException => HttpStatusCode.NotFound,
            NotImplementedException => HttpStatusCode.NotImplemented,
            TimeoutException => HttpStatusCode.RequestTimeout,
            _ => HttpStatusCode.InternalServerError
        };

    private static string GetUserFriendlyMessage(Exception exception) =>
        exception switch
        {
            FluentValidation.ValidationException validationEx => GetValidationErrorMessage(validationEx),
            ArgumentNullException => "Отсутствует обязательный параметр",
            ArgumentException => exception.Message,
            UnauthorizedAccessException => exception.Message,
            InvalidOperationException => exception.Message,
            KeyNotFoundException => "Запрашиваемый ресурс не найден",
            NotImplementedException => "Функциональность еще не реализована",
            TimeoutException => "Превышено время ожидания операции",
            _ => "Произошла внутренняя ошибка сервера"
        };

    private static string GetValidationErrorMessage(FluentValidation.ValidationException validationException)
    {
        var errors = validationException.Errors
            .Select(error => error.ErrorMessage)
            .ToList();

        return errors.Count == 1 
            ? errors.First() 
            : $"Обнаружены ошибки валидации: {string.Join("; ", errors)}";
    }
}