using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Application.DependencyInjection;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(ApplicationServiceRegistration).Assembly));

        services.AddValidatorsFromAssembly(typeof(ApplicationServiceRegistration).Assembly);

        return services;
    }
}