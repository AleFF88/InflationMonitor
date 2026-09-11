using FluentValidation;
using InflationMonitor.Application.Common.Behaviors;
using InflationMonitor.Application.Common.Interfaces;
using InflationMonitor.Application.Dtos;
using InflationMonitor.Application.Factories;
using InflationMonitor.Application.Queries.CalculateComparison;
using InflationMonitor.Application.Strategies;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace InflationMonitor.Application {
    public static class DependencyInjection {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services) {

            // Add FluentValidation validatiors to the DI container
            services.AddValidatorsFromAssemblyContaining<CalculateComparisonQueryValidator>();


            // Register MediatR and connect ValidationBehavior to the MediatR Execution Pipeline
            services.AddMediatR(cfg => {
                cfg.RegisterServicesFromAssembly(typeof(CalculateComparisonResponseDto).Assembly);
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            });


            // Register strategies and factories for financial instrument calculations
            services.AddScoped<IBatchFinancialInstrumentStrategy, InflationStrategy>();
            services.AddScoped<IBatchFinancialInstrumentStrategy, BatchCurrencyStrategy>();
            services.AddScoped<IFinancialInstrumentFactory, FinancialInstrumentFactory>();

            return services;
        }
    }
}
