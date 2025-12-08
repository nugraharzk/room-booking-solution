using System;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RoomBooking.Application.Bookings;
using RoomBooking.Application.Interfaces;
using RoomBooking.Infrastructure.Auth;
using RoomBooking.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Configuration
var configuration = builder.Configuration;

// Add services to the container.
builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        // Render enums as strings in JSON for clarity
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// OpenAPI / Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ProblemDetails for standardized error responses
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<RoomBooking.API.Infrastructure.GlobalExceptionHandler>();

// MediatR (scan Application assembly)
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<CreateRoomCommand>();
});

// EF Core DbContext (PostgreSQL via Npgsql)
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connStr = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connStr))
    {
        throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
    }
    options.UseNpgsql(connStr);
});

// Unit of Work
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();

// Authentication & Authorization (JWT + policies)
builder.Services.AddApiSecurity(configuration);

// CORS (optional, locked down; adjust origins as needed)
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithOrigins(
                "https://localhost:5201",
                "http://localhost:5200",
                "http://localhost:5173" // Vite default port
            );
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Swagger
    app.UseSwagger();
    app.UseSwaggerUI();

    // Auto-migrate and seed in Development
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // Initialize ensures Created and Migrated, then seeds data
        DbInitializer.Initialize(db);
    }
}

// Centralized exception handling with ProblemDetails
app.UseExceptionHandler();

    // app.UseHttpsRedirection();

// Security middleware
app.UseCors("DefaultCors");
app.UseAuthentication();
app.UseAuthorization();

// Controller routes
app.MapControllers();

// Simple health endpoint
app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    timestamp = DateTimeOffset.UtcNow
}));

app.Run();
