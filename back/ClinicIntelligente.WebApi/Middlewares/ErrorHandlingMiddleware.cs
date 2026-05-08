using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClinicIntelligente.WebApi.Middlewares;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ErrorHandlingMiddleware(
        RequestDelegate next,
        ILogger<ErrorHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = exception switch
        {
            KeyNotFoundException => HttpStatusCode.NotFound,
            InvalidOperationException => HttpStatusCode.BadRequest,
            ArgumentException => HttpStatusCode.BadRequest,
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            _ => HttpStatusCode.InternalServerError
        };

        LogException(exception, statusCode);

        var response = CreateErrorResponse(context, exception, statusCode);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(response,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        await context.Response.WriteAsync(json);
    }

    private void LogException(Exception exception, HttpStatusCode statusCode)
    {
        if ((int)statusCode >= 500)
        {
            _logger.LogError(exception, "Unhandled server error");
        }
        else
        {
            _logger.LogWarning(exception, "Handled exception");
        }
    }

    private object CreateErrorResponse(HttpContext context, Exception exception, HttpStatusCode statusCode)
    {
        return new
        {
            status = (int)statusCode,
            error = GetErrorMessage(exception, statusCode),
            timestamp = DateTime.UtcNow,
            path = context.Request.Path.Value,
            traceId = context.TraceIdentifier,
            details = _env.IsDevelopment() ? exception.Message : null
        };
    }

    private static string GetErrorMessage(Exception exception, HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.InternalServerError => "An internal server error occurred.",
            _ => exception.Message
        };
    }
}