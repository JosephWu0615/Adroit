using Adroit.Core.Interfaces;
using Adroit.Data;
using Adroit.Data.Repositories;
using Adroit.Infrastructure.Services;
using Adroit.Infrastructure.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure database based on connection string availability
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var useInMemory = string.IsNullOrEmpty(connectionString);

if (useInMemory)
{
    // Use in-memory storage (for local development without DB)
    builder.Services.AddSingleton<IUrlRepository, InMemoryUrlRepository>();
    Console.WriteLine("Using In-Memory storage");
}
else
{
    // Use SQL Server with Entity Framework
    builder.Services.AddDbContext<AdroitDbContext>(options =>
        options.UseSqlServer(connectionString));
    builder.Services.AddScoped<IUrlRepository, SqlUrlRepository>();
    Console.WriteLine("Using SQL Server database");
}

// Register other services
builder.Services.AddSingleton<IShortCodeGenerator, ShortCodeGenerator>();
builder.Services.AddScoped<IUrlService, UrlService>();

// Configure CORS for React frontend
var frontendUrl = builder.Configuration["AppSettings:FrontendUrl"] ?? "http://localhost:3000";
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "https://localhost:3000",
                frontendUrl)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Configure Swagger/OpenAPI
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Adroit URL Shortener API",
        Version = "v1",
        Description = "A high-performance URL shortening service with in-memory storage",
        Contact = new OpenApiContact
        {
            Name = "Adroit",
            Email = "support@adroit.com"
        }
    });

    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Apply database migrations on startup (if using SQL)
if (!useInMemory)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AdroitDbContext>();
    dbContext.Database.Migrate();
    Console.WriteLine("Database migrations applied");
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Adroit API V1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowReactApp");

// Serve static files (React frontend)
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();

// Map controllers
app.MapControllers();

// Map health check endpoint
app.MapHealthChecks("/health");

// SPA fallback - serve index.html for non-API routes
app.MapFallbackToFile("index.html");

// Log startup information
app.Logger.LogInformation("Adroit URL Shortener API started");
app.Logger.LogInformation("Swagger UI available at: /swagger");
app.Logger.LogInformation("Health check available at: /health");

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
