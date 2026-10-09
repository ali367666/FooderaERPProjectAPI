using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Application.DependencyInjection;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ApplicationServiceRegistration).Assembly);
            cfg.AddOpenBehavior(typeof(Application.Common.Behaviors.RestaurantOwnershipBehavior<,>));
            cfg.AddOpenBehavior(typeof(Application.Common.Behaviors.OrderPolicyBehavior<,>));
        });

        services.AddScoped<Application.Common.Interfaces.Abstracts.Services.IStaffCodeResolver, Application.Auth.Services.StaffCodeResolver>();

        services.AddValidatorsFromAssembly(typeof(ApplicationServiceRegistration).Assembly);

        return services;
    }
}