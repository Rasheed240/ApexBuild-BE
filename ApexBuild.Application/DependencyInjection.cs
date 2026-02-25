using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ApexBuild.Application.Common.Behaviours;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ApexBuild.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Register MediatR
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            });

            // Register FluentValidation
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            // Register MediatR Pipeline Behaviors
            // Order matters: Validation runs first, then caching (so we never cache invalid requests)
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehaviour<,>));

            // Register AutoMapper
            services.AddAutoMapper(Assembly.GetExecutingAssembly());

            return services;
        }
    }
}