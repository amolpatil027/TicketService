using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using System.Text.Json;
using TicketAgeApi.Middleware;
using TicketAgeApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddControllers();
builder.Services.AddSingleton<TicketService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Ticket Age API",
        Version = "v1",
        Description = "Uploads and merges multiple incident Excel files, calculates ticket age from Opened date against the server system date, and downloads merged tickets older than 7 days."
    });
});

// 1. Add health checks to the DI container
builder.Services.AddHealthChecks()
    .AddCheck("Database", () =>
        HealthCheckResult.Healthy("Database is reachable."))
    .AddCheck("StorageService", () =>
        HealthCheckResult.Degraded("Storage latency is higher than expected."));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ticket Age API v1");
});

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        var responsePayload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            entries = report.Entries.Select(entry => new
            {
                check = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                durationMs = entry.Value.Duration.TotalMilliseconds,
                exception = entry.Value.Exception?.Message,
                data = entry.Value.Data
            })
        };

        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            responsePayload,
            new JsonSerializerOptions { WriteIndented = true });
    }
});

app.Run();
