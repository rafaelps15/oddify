using Anthropic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Oddify.Common.Application.EventBus;
using Oddify.Common.Infrastructure.Inbox;
using Oddify.Common.Infrastructure.Outbox;
using Oddify.Common.Presentation.Endpoints;
using Presentation = Oddify.Modules.Analise.Presentation;
using Oddify.Modules.Analise.Application.Abstractions.Data;
using Oddify.Modules.Analise.Application.Abstractions.Llm;
using Oddify.Modules.Analise.Domain.Analises;
using Oddify.Modules.Analise.Domain.Fixtures;
using Oddify.Modules.Analise.Infrastructure.Analises;
using Oddify.Modules.Analise.Infrastructure.Database;
using Oddify.Modules.Analise.Infrastructure.Fixtures;
using Oddify.Modules.Analise.Infrastructure.Inbox;
using Oddify.Modules.Analise.Infrastructure.Llm;
using Oddify.Modules.Analise.Infrastructure.PublicApi;
using Oddify.Modules.Analise.PublicApi;

namespace Oddify.Modules.Analise.Infrastructure;

public static class AnaliseModule
{
    public static IServiceCollection AddAnaliseModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEndpoints(Presentation.AssemblyReference.Assembly);

        // Consome os integration events de Fixtures (LigaAtualizada, PartidaAgendada,
        // PartidaEncerrada, CotacaoColetada) pra manter o mirror local — ver CLAUDE.md §10/§17.
        services.AddIntegrationEventHandlers(Presentation.AssemblyReference.Assembly);

        services.AddInfrastructure(configuration);
        return services;
    }

    // Assina, um por um, cada integration event que este módulo consome — mesmo padrão de
    // ApostasModule.Initialize/UsersModule.Initialize (§10/§12). Chamado de Program.cs depois de
    // builder.Build(), quando o IEventBus já existe no container.
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

        services.AddDbContext<AnaliseDbContext>((sp, options) =>
            options
                .UseNpgsql(databaseConnectionString, npgsqlOptions => npgsqlOptions
                    .MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Analise)
                    .EnableRetryOnFailure())
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(sp.GetRequiredService<InsertOutboxMessagesInterceptor>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AnaliseDbContext>());

        services.AddScoped<IAnaliseDePartidaRepository, AnaliseDePartidaRepository>();

        services.AddScoped<ILigaRepository, LigaRepository>();
        services.AddScoped<IPartidaRepository, PartidaRepository>();
        services.AddScoped<ICotacaoRepository, CotacaoRepository>();

        services.AddSingleton<IAnaliseApi, AnaliseApi>();

        services.AddSingleton<AnthropicClient>(_ => new AnthropicClient());
        services.AddScoped<IClaudeAvaliadorCriticoService, ClaudeAvaliadorCriticoService>();

        services.AddOutboxProcessor(Schemas.Analise);
        services.AddInboxProcessor(Schemas.Analise, Presentation.AssemblyReference.Assembly);
    }
}
