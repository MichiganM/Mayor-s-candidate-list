using FluentValidation;
using MayorElection.Services.DTOs;
using MayorElection.Services.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MayorElection.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddElectionServices(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<UserSession>();
        services.AddScoped<AuthService>();
        services.AddScoped<IValidator<CandidateDto>, CandidateValidator>();
        services.AddScoped<CandidateService>();
        services.AddScoped<ListOrderService>();
        services.AddScoped<ExportService>();
        return services;
    }
}
