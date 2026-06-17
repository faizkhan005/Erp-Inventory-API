using ERPInventoryApi.API;
using ERPInventoryApi.API.Middleware;
using ERPInventoryApi.API.OpenAPI;
using ERPInventoryApi.Application.Interfaces;
using ERPInventoryApi.Application.Services;
using ERPInventoryApi.Infrastructure.Data;
using ERPInventoryApi.Infrastructure.Repositories;
using ERPInventoryApi.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using StackExchange.Redis;
using System.Text;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting ERP Inventory API....");

    var builder = WebApplication.CreateBuilder(args);

    //use serial logg for all logging
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .WriteTo.Console());

    //Add Service
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    });

    // PostgreSQL + EF Core
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    // JWT Authentication 
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(secretKey),
            ClockSkew = TimeSpan.Zero // no grace period on expiry
        };
    });

    builder.Services.AddAuthorization();


    // Dependency Injection for Repositories
    builder.Services.AddScoped<IProductRepository, ProductRepository>();
    builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
    builder.Services.AddScoped<IWarehouseRepository, WarehouseRepository>();
    builder.Services.AddScoped<ICategoryService, CategoryService>();

    //DI for Services
    builder.Services.AddScoped<IProductService, ProductService>();
    builder.Services.AddScoped<IWarehouseService, WarehouseService>();
    builder.Services.AddScoped<ICategoryService, CategoryService>();
    builder.Services.AddScoped<IAuthService, AuthService>();

    var redisConnection = builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException("Redis connection string is missing.");

    builder.Services.AddSingleton<IConnectionMultiplexer>(
        ConnectionMultiplexer.Connect(redisConnection));

    builder.Services.AddSingleton<ICacheService, CacheService>();

    // Health checks
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<AppDbContext>();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    app.UseMiddleware<CorrelationIdMiddleware>();    // 1. Attach trace ID first
    app.UseMiddleware<RequestTimingMiddleware>();    // 2. Start clock
    app.UseMiddleware<GlobalExceptionMiddleware>();  // 3. Catch all exceptions
    app.UseMiddleware<RequestLoggingMiddleware>();   // 4. Log request/response


    app.UseSerilogRequestLogging();                  // Serilog's built-in HTTP log
    app.UseAuthentication();                         // 5. Validate JWT
    app.UseAuthorization();                          // 6. Enforce [Authorize]
    app.MapControllers();
    app.MapHealthChecks("/health");

    // Database seeding 
    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seederLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        await DatabaseSeeder.SeedAsync(db, seederLogger);
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}