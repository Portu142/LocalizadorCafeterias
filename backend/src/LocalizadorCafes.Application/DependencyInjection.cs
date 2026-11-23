using FluentValidation;
using LocalizadorCafes.Application.Cafes;
using LocalizadorCafes.Application.Cafes.Validators;
using Microsoft.Extensions.DependencyInjection;

namespace LocalizadorCafes.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CafeService>();
        services.AddScoped<Admin.AdminService>();

        // Visitantes en tiempo real: una única instancia en memoria para toda la API.
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<Presence.PresenceTracker>();
        services.AddValidatorsFromAssemblyContaining<CreateReviewRequestValidator>();
        return services;
    }
}
