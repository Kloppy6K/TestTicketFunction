using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using CloudEngineerFunction;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        // Hier registrieren wir unsere Azure SQL Datenbank mit EF Core
        var connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));
    })
    .Build();

// --- NEU: Automatisch beim Start die Tabelle in Azure anlegen, falls sie fehlt ---
using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
}
// -------------------------------------------------------------------------------

host.Run();