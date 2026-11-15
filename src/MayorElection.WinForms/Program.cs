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
                services.AddTransient<LoginForm>();
                services.AddTransient<RegisterForm>();
                services.AddTransient<TwoFactorSetupForm>();
                services.AddTransient<MainForm>();
                services.AddTransient<CandidateEditForm>();
                services.AddScoped<Func<RegisterForm>>(provider => () => provider.GetRequiredService<RegisterForm>());
                services.AddScoped<Func<TwoFactorSetupForm>>(provider => () => provider.GetRequiredService<TwoFactorSetupForm>());
                services.AddScoped<Func<CandidateEditForm>>(provider => () => provider.GetRequiredService<CandidateEditForm>());
                services.AddSingleton<ElectionApplicationContext>();
            })
            .Build();

        try
        {
            using (var scope = host.Services.CreateScope())
                scope.ServiceProvider.GetRequiredService<DatabaseInitializer>()
                    .InitializeAsync(BootstrapConsole.Display).GetAwaiter().GetResult();
            Application.Run(host.Services.GetRequiredService<ElectionApplicationContext>());
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Application startup failed");
            MessageBox.Show("Не удалось запустить приложение. Проверьте доступ к базе данных и журнал ошибок.",
                "Выборы мэра", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
