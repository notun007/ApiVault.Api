using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using ApiVault.Api.Middleware;
using ApiVault.Application;
using ApiVault.Infrastructure;
using ApiVault.Infrastructure.Options;
using ApiVault.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Kestrel configuration
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize =
        builder.Configuration.GetValue<long?>(
            "Kestrel:Limits:MaxRequestBodySize")
        ?? 10_485_760;
});

// Controllers and JSON configuration
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
    });

builder.Services.AddProblemDetails();

// Application services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// JWT configuration
var jwt = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>()
    ?? new JwtOptions();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata =
            !builder.Environment.IsDevelopment();

        options.SaveToken = false;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwt.Issuer,

                ValidateAudience = true,
                ValidAudience = jwt.Audience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(
                        jwt.SigningKey ?? string.Empty)),

                ValidateLifetime = true,
                RequireExpirationTime = true,

                ClockSkew = TimeSpan.FromMinutes(1),

                NameClaimType = ClaimTypes.Name,
                RoleClaimType = ClaimTypes.Role
            };
    });

builder.Services.AddAuthorization();

// CORS configuration
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ApiVaultUi", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy
                .WithOrigins("http://localhost:4200")
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
        else if (builder.Environment.IsDevelopment())
        {
            policy
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

var app = builder.Build();

// Global exception handling
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Swagger is available in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "ApiVault API v1");

        options.RoutePrefix = "swagger";
    });
}

// HTTP pipeline
app.UseHttpsRedirection();

app.UseCors("ApiVaultUi");

app.UseAuthentication();
app.UseAuthorization();

// Root endpoint
app.MapGet("/", () =>
    Results.Ok(new
    {
        name = "ApiVault.Api",
        status = "running",
        swagger = "/swagger",
        health = "/health"
    }))
    .AllowAnonymous();

// Health endpoint
app.MapGet("/health", () =>
    Results.Ok(new
    {
        status = "healthy",
        utc = DateTime.UtcNow
    }))
    .AllowAnonymous();

// Controller endpoints
app.MapControllers();

// Database initialization
await using (var scope = app.Services.CreateAsyncScope())
{
    var seeder = scope.ServiceProvider
        .GetRequiredService<DatabaseSeeder>();

    await seeder.InitializeAsync();
}

await app.RunAsync();

public partial class Program
{
}