using JavaMaster.Api.Data;
using JavaMaster.Api.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi();

var app = builder.Build();

// Apply migrations and seed data on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        await db.Database.MigrateAsync();
        await DbSeeder.SeedAsync(db);
        await LessonContent.SeedAsync(db);
    }
    catch (Exception ex)
    {
        // A missing or misconfigured database is the most common reason the app
        // will not start, so say what to do instead of printing a stack trace.
        Console.Error.WriteLine();
        Console.Error.WriteLine("  Java Master could not reach SQL Server, so it cannot start.");
        Console.Error.WriteLine();
        Console.Error.WriteLine($"  Connection string: {builder.Configuration.GetConnectionString("Default")}");
        Console.Error.WriteLine($"  Error: {ex.Message}");
        Console.Error.WriteLine();
        Console.Error.WriteLine("  Things to check:");
        Console.Error.WriteLine("    1. SQL Server is installed and running (open services.msc).");
        Console.Error.WriteLine("    2. The connection string names the right instance:");
        Console.Error.WriteLine("         Server=localhost              default instance");
        Console.Error.WriteLine("         Server=localhost\\SQLEXPRESS   SQL Server Express");
        Console.Error.WriteLine("         Server=(localdb)\\MSSQLLocalDB LocalDB");
        Console.Error.WriteLine("    3. Your Windows account can sign in to it (Windows authentication).");
        Console.Error.WriteLine();
        Console.Error.WriteLine("  The JavaMaster database is created and seeded for you, so there is");
        Console.Error.WriteLine("  nothing else to set up by hand.");
        Console.Error.WriteLine();
        Environment.ExitCode = 1;
        return;
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // While developing, never let the browser cache a page or a script, so an
    // edit shows up on the next refresh instead of the next hard reload.
    app.Use(async (context, next) =>
    {
        context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate, max-age=0";
        context.Response.Headers.Pragma = "no-cache";
        await next();
    });
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();

app.Run();