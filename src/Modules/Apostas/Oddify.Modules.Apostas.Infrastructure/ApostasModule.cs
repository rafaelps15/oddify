using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Oddify.Common.Application.EventBus;
using Oddify.Common.Infrastructure.Inbox;
using Oddify.Common.Infrastructure.Outbox;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Modules.Apostas.Application.Abstractions.Data;
using Oddify.Modules.Apostas.Domain.AnalisesDisponiveis;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;
using Oddify.Modules.Apostas.Domain.Bancas;
using Oddify.Modules.Apostas.Domain.JornadasDeAlavancagem;
using Oddify.Modules.Apostas.Domain.MovimentacoesDaBanca;
using Oddify.Modules.Apostas.Domain.PassosDaJornada;
using Oddify.Modules.Apostas.Domain.PernasDeAposta;
using Oddify.Modules.Apostas.Infrastructure.AnalisesDisponiveis;
using Oddify.Modules.Apostas.Infrastructure.ApostasMultiplas;
using Oddify.Modules.Apostas.Infrastructure.Bancas;
using Oddify.Modules.Apostas.Infrastructure.Database;
using Oddify.Modules.Apostas.Infrastructure.Inbox;
using Oddify.Modules.Apostas.Infrastructure.JornadasDeAlavancagem;
using Oddify.Modules.Apostas.Infrastructure.MovimentacoesDaBanca;
using Oddify.Modules.Apostas.Infrastructure.PassosDaJornada;
using Oddify.Modules.Apostas.Infrastructure.PernasDeAposta;
using Presentation = Oddify.Modules.Apostas.Presentation;

namespace Oddify.Modules.Apostas.Infrastructure;

public static class ApostasModule
{
    public static IServiceCollection AddApostasModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEndpoints(Presentation.AssemblyReference.Assembly);
        services.AddIntegrationEventHandlers(Presentation.AssemblyReference.Assembly, Schemas.Apostas);
        services.AddInfrastructure(configuration);
        return services;
    }

    // Um IntegrationEventConsumer<T> (Infrastructure/Inbox) por tipo de integration event que
    // este módulo consome — achado por reflexão a partir de quem implementa
    // IIntegrationEventHandler<T> no assembly Presentation, em vez de registrar cada consumer à
    // mão um por um.
    public static void ConfigureConsumers(IRegistrationConfigurator registrationConfigurator)
    {
        IEnumerable<Type> integrationEventTypes = Presentation.AssemblyReference.Assembly.GetTypes()
            .Where(type => !type.IsAbstract && type.IsAssignableTo(typeof(IIntegrationEventHandler)) && type != typeof(IIntegrationEventHandler))
            .Select(handlerType => handlerType.GetInterfaces()
                .Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>))
                .GetGenericArguments()[0])
            .Distinct();

        foreach (Type integrationEventType in integrationEventTypes)
        {
            registrationConfigurator.AddConsumer(typeof(IntegrationEventConsumer<>).MakeGenericType(integrationEventType));
        }
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
                .AddInterceptors(sp.GetRequiredService<InsertOutboxMessagesInterceptor>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApostasDbContext>());

        services.AddScoped<IBancaRepository, BancaRepository>();
        services.AddScoped<IApostaMultiplaRepository, ApostaMultiplaRepository>();
        services.AddScoped<IPernaDeApostaRepository, PernaDeApostaRepository>();
        services.AddScoped<IAnaliseDisponivelParaApostaRepository, AnaliseDisponivelParaApostaRepository>();
        services.AddScoped<IMovimentacaoDaBancaRepository, MovimentacaoDaBancaRepository>();
        services.AddScoped<IJornadaDeAlavancagemRepository, JornadaDeAlavancagemRepository>();
        services.AddScoped<IPassoDaJornadaRepository, PassoDaJornadaRepository>();
        services.AddScoped<IFaixaDeMetaCatalogoRepository, FaixaDeMetaCatalogoRepository>();

        services.AddOutboxProcessor(Schemas.Apostas);
        services.AddInboxProcessor(Schemas.Apostas, Presentation.AssemblyReference.Assembly);
    }
}
