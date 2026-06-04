using Microsoft.EntityFrameworkCore;
using MyAnimeList.Backend.Data;
using MyAnimeList.Backend.Repositories;
using MyAnimeList.Backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Add DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AnimeDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add HTTP Client for Jikan API (for cron job sync only)
builder.Services.AddHttpClient<JikanApiClient>();

// Add SQL migration service (replaces EF migrations)
builder.Services.AddScoped<ISqlMigrationService, SqlMigrationService>();

// Add database initialization service (kept for compatibility, but now uses SQL migrations)
builder.Services.AddScoped<DatabaseInitializationService>();
builder.Services.AddScoped<IAnimeService, AnimeService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ILibraryService, LibraryService>();

builder.Services.AddScoped<IAnimeRepository, AnimeRepository>();
builder.Services.AddScoped<ILibraryRepository, LibraryRepository>();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";

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
        ValidIssuer = jwtSettings["Issuer"] ?? "MyAnimeList.Backend",
        ValidAudience = jwtSettings["Audience"] ?? "MyAnimeList.Frontend",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

// Add CORS for Android app
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Apply SQL migrations on startup
using (var scope = app.Services.CreateScope())
{
    var sqlMigrationService = scope.ServiceProvider.GetRequiredService<ISqlMigrationService>();
    await sqlMigrationService.ApplyMigrationsAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Remove or comment out HTTPS redirect for local HTTP access
// app.UseHttpsRedirection();

app.UseCors("AllowAllOrigins");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();