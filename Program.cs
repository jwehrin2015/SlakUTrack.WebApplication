using Microsoft.EntityFrameworkCore;
using SlakUTrack.WebApplication.Data;
using SlakUTrack.WebApplication.Services;

var builder = WebApplication.CreateBuilder(args);

var databasePath = builder.Configuration["SLAKUTRACK_DB_PATH"];
if (string.IsNullOrWhiteSpace(databasePath))
{
    databasePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SLAK-U-Track-Web",
        "attendance.db");
}

databasePath = Path.GetFullPath(databasePath);
var databaseDirectory = Path.GetDirectoryName(databasePath)
    ?? throw new InvalidOperationException("Could not determine the database directory.");
Directory.CreateDirectory(databaseDirectory);

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite($"Data Source={databasePath}"));
builder.Services.AddSingleton(new DatabaseSettings(databasePath));

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var database = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    await using var context = await database.CreateDbContextAsync();
    await context.Database.EnsureCreatedAsync();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapAttendanceApi();
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;
