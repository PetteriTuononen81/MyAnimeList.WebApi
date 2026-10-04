using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MyAnimeList.Backend.Database.Repositories;
using MyAnimeList.Backend.Services;
using MyAnimeList.Backend.Services.ApiClient;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Add HTTP Client for Jikan or Tenrai API (for cron job sync only, currently manual call)
builder.Services.AddScoped<IAnimeApiClient, TenraiApiClient>(); // Current API client for anime data
builder.Services.AddHttpClient<IAniListApiClient, AniListApiClient>(client =>
{
    client.BaseAddress = new Uri("https://graphql.anilist.co");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Add SQL migration service
builder.Services.AddScoped<ISqlMigrationService, SqlMigrationService>();

// Add services
builder.Services.AddScoped<IAnimeService, AnimeService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ILibraryService, LibraryService>();
builder.Services.AddScoped<IAnimeMetadataService, AnimeMetadataService>();
builder.Services.AddHttpClient<IAiImportService, AiImportService>(client =>
{
    client.BaseAddress = new Uri("http://host.docker.internal:11434/");
});
builder.Services.AddScoped<ISearchService, SearchService>();

// Add repositories 
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IAnimeRepository, AnimeRepository>();
builder.Services.AddScoped<ILibraryRepository, LibraryRepository>();
builder.Services.AddScoped<IAnimeMetadataRepository, AnimeMetadataRepository>();

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