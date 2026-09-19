using Microsoft.OpenApi.Models;
using TicketAgeApi.Services;

var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ticket Age API v1");
});

app.MapControllers();

app.Run();
