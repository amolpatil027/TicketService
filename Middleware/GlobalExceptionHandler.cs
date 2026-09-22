using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TicketAgeApi.Exceptions;

namespace TicketAgeApi.Middleware
{
    public sealed class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly IHostEnvironment _env;
        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }
        public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);

            var (statusCode, title, type) = exception switch
            {
                NotFoundException => (
                    StatusCodes.Status404NotFound,
                    "Resource Not Found",
                    "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.5"
                ),
                BadRequestException or ArgumentException => (
                    StatusCodes.Status400BadRequest,
                    "Bad Request",
                    "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.1"
                ),
                _ => (
                    StatusCodes.Status500InternalServerError,
                    "An unexpected internal error occurred.",
                    "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.1"
                )
            };

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Type = type,
                Detail = _env.IsDevelopment()
                    ? exception.Message
                    : (statusCode == StatusCodes.Status500InternalServerError ? "Please contact support." : exception.Message),
                Instance = httpContext.Request.Path
            };

            problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

            if (_env.IsDevelopment() && exception.StackTrace is not null)
            {
                problemDetails.Extensions["stackTrace"] = exception.StackTrace;
            }

            httpContext.Response.StatusCode = statusCode;
            httpContext.Response.ContentType = "application/problem+json";

            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }
    }
}
