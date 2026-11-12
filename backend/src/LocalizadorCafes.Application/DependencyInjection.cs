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
        services.AddValidatorsFromAssemblyContaining<CreateReviewRequestValidator>();
        return services;
    }
}
