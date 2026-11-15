using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using MayorElection.Data;
using MayorElection.Services;

namespace MayorElection.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        using var host = Host.CreateDefaultBuilder()
            .UseContentRoot(AppContext.BaseDirectory)
            .UseSerilog((context, configuration) => configuration
                .MinimumLevel.Information()
                .WriteTo.File(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MayorElection", "logs", "app-.log"), rollingInterval: RollingInterval.Day))
            .ConfigureServices((context, services) =>
            {
                services.AddElectionData(context.Configuration);
                services.AddElectionServices();
                services.AddTransient<ScaffoldForm>();
            })
            .Build();

        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<DatabaseInitializer>()
            .InitializeAsync(BootstrapConsole.Display).GetAwaiter().GetResult();
        Application.Run(scope.ServiceProvider.GetRequiredService<ScaffoldForm>());
    }
}
