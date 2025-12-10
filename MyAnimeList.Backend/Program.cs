using Microsoft.EntityFrameworkCore;
using MyAnimeList.Backend.Data;
using MyAnimeList.Backend.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Add DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AnimeDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add HTTP Client for Jikan API (for cron job sync only)
builder.Services.AddHttpClient<JikanApiService>();

// Add database initialization service
builder.Services.AddScoped<DatabaseInitializationService>();

var app = builder.Build();

// Initialize database on startup
using (var scope = app.Services.CreateScope())
{
    var dbInitializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializationService>();
    await dbInitializer.InitializeAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();