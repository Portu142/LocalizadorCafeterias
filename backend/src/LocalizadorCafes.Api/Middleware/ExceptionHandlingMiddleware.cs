using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace LocalizadorCafes.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException exception)
        {
            _logger.LogWarning(
                "Error de validación en {Path}: {Errors}",
                context.Request.Path,
                string.Join("; ", exception.Errors.Select(error => $"{error.PropertyName}: {error.ErrorMessage}")));

            context.Response.StatusCode = StatusCodes.Status400BadRequest;

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Error de validación",
                Detail = "Uno o más campos no son válidos.",
                Instance = context.Request.Path
            };

            problem.Extensions["errors"] = exception.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(error => error.ErrorMessage).ToArray());

            await context.Response.WriteAsJsonAsync(
                problem,
                options: null,
                contentType: "application/problem+json");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error no controlado en {Path}", context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Error interno del servidor",
                Detail = "Ha ocurrido un error inesperado.",
                Instance = context.Request.Path
            };

            await context.Response.WriteAsJsonAsync(
                problem,
                options: null,
                contentType: "application/problem+json");
        }
    }
}
