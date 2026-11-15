using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MayorElection.Data;

/// <summary>Use -- --settings path/to/appsettings.json for an alternate design-time configuration.</summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var settingsPath = args.Length == 2 && args[0] == "--settings"
            ? args[1] : FindSettings();
        using var document = JsonDocument.Parse(File.ReadAllText(settingsPath));
        var connection = document.RootElement.GetProperty("ConnectionStrings")
            .GetProperty("ElectionDatabase").GetString();
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("ElectionDatabase connection string is empty.");
        return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
    }

    private static string FindSettings()
    {
        for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory is not null;
             directory = directory.Parent)
        {
            var path = Path.Combine(directory.FullName, "src", "MayorElection.WinForms", "appsettings.json");
            if (File.Exists(path)) return path;
        }
        throw new FileNotFoundException("Specify appsettings.json with -- --settings <absolute path>.");
    }
}
