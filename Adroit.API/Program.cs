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
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Adroit API V1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseCors("AllowReactApp");

// Short URL redirect middleware - runs BEFORE static files and routing
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.TrimStart('/') ?? "";

    // Skip reserved paths
    if (string.IsNullOrEmpty(path) ||
        path.StartsWith("api", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("swagger", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("health", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("static", StringComparison.OrdinalIgnoreCase) ||
        path.Contains('.') ||
        path.Contains('/'))
    {
        await next();
        return;
    }

    // Try to resolve as short code
    var urlService = context.RequestServices.GetService<IUrlService>();
    if (urlService != null)
    {
        var longUrl = await urlService.GetLongUrlAsync(path);
        if (longUrl != null)
        {
            // Record click
            _ = Task.Run(async () => await urlService.RecordClickAsync(path));
            context.Response.Redirect(longUrl, permanent: false);
            return;
        }
    }

    await next();
});

app.UseRouting();
app.UseAuthorization();

// Serve static files (React frontend)
app.UseDefaultFiles();
app.UseStaticFiles();

// Map controllers
app.MapControllers();

// Map health check endpoint
app.MapHealthChecks("/health");

// SPA fallback
app.MapFallbackToFile("index.html");

// Log startup information
app.Logger.LogInformation("Adroit URL Shortener API started");
app.Logger.LogInformation("Swagger UI available at: /swagger");
app.Logger.LogInformation("Health check available at: /health");

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
