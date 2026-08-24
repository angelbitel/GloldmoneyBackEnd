using System.Net;
using FluentValidation;
using GoldmoneyBackend.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace GoldmoneyBackend.Api.Middleware;

public sealed class GlobalExceptionHandler : IExceptionHandler
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
        _logger.LogError(exception, "Unhandled exception while processing request.");

        var (statusCode, title, detail, errors) = MapException(exception);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        if (errors is not null)
        {
            problemDetails.Extensions["errors"] = errors;
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static (int StatusCode, string Title, string Detail, IDictionary<string, string[]>? Errors) MapException(Exception exception)
    {
        return exception switch
        {
            ValidationException validationException => (
                StatusCodes.Status422UnprocessableEntity,
                "Validation error",
                "One or more validation errors occurred.",
                validationException.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())),

            DomainValidationException domainValidation => (
                StatusCodes.Status400BadRequest,
                "Bad request",
                domainValidation.Message,
                null),

            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "You are not authorized to access this resource.",
                null),

            System.Security.SecurityException => (
                StatusCodes.Status403Forbidden,
                "Forbidden",
                "Access to this resource is forbidden.",
                null),

            NotFoundDomainException notFound => (
                StatusCodes.Status404NotFound,
                "Not found",
                notFound.Message,
                null),

            ConflictDomainException conflict => (
                StatusCodes.Status409Conflict,
                "Conflict",
                conflict.Message,
                null),

            _ => (
                (int)HttpStatusCode.InternalServerError,
                "Internal server error",
                "An unexpected error occurred.",
                null)
        };
    }
}