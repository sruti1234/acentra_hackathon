using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using MultiTenantInventory.Api.Configuration;
using MultiTenantInventory.Api.Data;
using MultiTenantInventory.Api.Interfaces;
using MultiTenantInventory.Api.Middleware;
using MultiTenantInventory.Api.Models;
using MultiTenantInventory.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------- Services (Dependency Injection) ----------

builder.Services.AddControllers();

// Swagger with a JWT "Authorize" button and an X-Tenant-ID box on tenant endpoints.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Multi-Tenant Inventory API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the token from POST /api/auth/login"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });

    options.OperationFilter<TenantHeaderOperationFilter>();
});

// PostgreSQL via EF Core. The password comes from user-secrets or the
// ConnectionStrings__DefaultConnection environment variable.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Current request's tenant — Scoped = one per HTTP request.
builder.Services.AddScoped<ITenantContext, TenantContext>();

// Authentication (JWT) + password hashing (ASP.NET Core Identity's PBKDF2 hasher).
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// AWS S3 (optional — the app runs without it).
builder.Services.Configure<S3Settings>(builder.Configuration.GetSection("AWS"));
builder.Services.AddSingleton<IS3Service, S3Service>();

// CORS: allow the React dev server (Vite) to call this API from the browser.
const string FrontendCorsPolicy = "FrontendCorsPolicy";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? new[] { "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// ---------- Development seed data ----------

if (app.Environment.IsDevelopment())
{
    await DbSeeder.SeedAsync(app.Services, app.Logger);
}

// ---------- HTTP request pipeline ----------
// Order matters: CORS -> JWT authentication -> authorization -> tenant validation -> controllers.

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(FrontendCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<TenantMiddleware>();

app.MapControllers();

app.Run();
