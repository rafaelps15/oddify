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
        services.AddIntegrationEventHandlers(Presentation.AssemblyReference.Assembly);
        services.AddInfrastructure(configuration);
        return services;
    }

    // Assina, um por um, cada integration event que este módulo consome — espelha
    // EventsBusStartup.Initialize do projeto de referência (Modular Monolith with DDD). Os tipos
    // ainda são achados por reflexão (a partir de quem implementa IIntegrationEventHandler<T> no
    // assembly Presentation) em vez de citados por nome aqui: Infrastructure não pode referenciar
    // o projeto IntegrationEvents de outro módulo (essa exceção é só de Presentation, ver
    // CLAUDE.md §2/§10) — só Presentation, que já referencia esses tipos pra implementar os
    // consumers de negócio, sabe o nome deles em tempo de compilação. Chamado de Program.cs depois
    // de builder.Build(), quando o IEventBus já existe no container.
    public static void Initialize(IServiceProvider serviceProvider)
    {
        IEventBus eventBus = serviceProvider.GetRequiredService<IEventBus>();

        IEnumerable<Type> integrationEventTypes = Presentation.AssemblyReference.Assembly.GetTypes()
            .Where(type => !type.IsAbstract && type.IsAssignableTo(typeof(IIntegrationEventHandler)) && type != typeof(IIntegrationEventHandler))
            .Select(handlerType => handlerType.GetInterfaces()
                .Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>))
                .GetGenericArguments()[0])
            .Distinct();

        foreach (Type integrationEventType in integrationEventTypes)
        {
            Type genericHandlerType = typeof(IntegrationEventGenericHandler<>).MakeGenericType(integrationEventType);
            var genericHandler = (IIntegrationEventHandler)Activator.CreateInstance(genericHandlerType, serviceProvider)!;

            eventBus.Subscribe(integrationEventType, genericHandler);
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
