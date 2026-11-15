using MayorElection.Data.Repositories;
using MayorElection.Domain.Entities;
using MayorElection.Domain.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MayorElection.Data;

public static class DependencyInjection
{
    public static IServiceCollection AddElectionData(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("ElectionDatabase")
            ?? throw new InvalidOperationException("ConnectionStrings:ElectionDatabase is required.");
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connection));
        services.AddScoped<ICandidateRepository, CandidateRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<DatabaseInitializer>();
        services.AddLogging();
        services.AddDataProtection();
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequiredLength = 12;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
        }).AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();
        services.Configure<PasswordHasherOptions>(options => options.IterationCount = 210_000);
        return services;
    }
}
