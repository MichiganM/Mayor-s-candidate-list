using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

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
            .ConfigureServices(services => services.AddTransient<ScaffoldForm>())
            .Build();

        using var scope = host.Services.CreateScope();
        Application.Run(scope.ServiceProvider.GetRequiredService<ScaffoldForm>());
    }
}
