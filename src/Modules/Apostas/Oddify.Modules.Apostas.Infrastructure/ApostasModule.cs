using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Oddify.Common.Infrastructure.Interceptors;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Modules.Apostas.Application.Abstractions.Data;
using Oddify.Modules.Apostas.Domain.AnalisesDisponiveis;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;
using Oddify.Modules.Apostas.Domain.Bancas;
using Oddify.Modules.Apostas.Domain.MovimentacoesDaBanca;
using Oddify.Modules.Apostas.Domain.PernasDeAposta;
using Oddify.Modules.Apostas.Infrastructure.AnalisesDisponiveis;
using Oddify.Modules.Apostas.Infrastructure.ApostasMultiplas;
using Oddify.Modules.Apostas.Infrastructure.Bancas;
using Oddify.Modules.Apostas.Infrastructure.Database;
using Oddify.Modules.Apostas.Infrastructure.MovimentacoesDaBanca;
using Oddify.Modules.Apostas.Infrastructure.PernasDeAposta;
using Oddify.Modules.Apostas.Presentation.IntegrationEvents;

namespace Oddify.Modules.Apostas.Infrastructure;

public static class ApostasModule
{
    public static IServiceCollection AddApostasModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEndpoints(Presentation.AssemblyReference.Assembly);
        services.AddInfrastructure(configuration);
        return services;
    }

    public static void ConfigureConsumers(IRegistrationConfigurator registrationConfigurator)
    {
        registrationConfigurator.AddConsumer<AnaliseConfirmadaIntegrationEventConsumer>();
        registrationConfigurator.AddConsumer<EmailVerifiedIntegrationEventConsumer>();
    }

    private static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        string databaseConnectionString = configuration.GetConnectionString("Database")!;

        services.AddDbContext<ApostasDbContext>((sp, options) =>
            options
                .UseNpgsql(databaseConnectionString, npgsqlOptions => npgsqlOptions
                    .MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Apostas)
                    .EnableRetryOnFailure())
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(sp.GetRequiredService<PublishDomainEventsInterceptor>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApostasDbContext>());

        services.AddScoped<IBancaRepository, BancaRepository>();
        services.AddScoped<IApostaMultiplaRepository, ApostaMultiplaRepository>();
        services.AddScoped<IPernaDeApostaRepository, PernaDeApostaRepository>();
        services.AddScoped<IAnaliseDisponivelParaApostaRepository, AnaliseDisponivelParaApostaRepository>();
        services.AddScoped<IMovimentacaoDaBancaRepository, MovimentacaoDaBancaRepository>();
    }
}
