using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Oddify.Common.Infrastructure.Outbox;
using Oddify.Common.Presentation.Endpoints;
using Presentation = Oddify.Modules.Fixtures.Presentation;
using Oddify.Modules.Fixtures.Application.Abstractions.Data;
using Oddify.Modules.Fixtures.Application.Abstractions.ExternalData;
using Oddify.Modules.Fixtures.Domain.Cotacoes;
using Oddify.Modules.Fixtures.Domain.Equipes;
using Oddify.Modules.Fixtures.Domain.Escalacoes;
using Oddify.Modules.Fixtures.Domain.EscalacoesDeJogador;
using Oddify.Modules.Fixtures.Domain.EstatisticasDeEquipe;
using Oddify.Modules.Fixtures.Domain.EstatisticasDeJogador;
using Oddify.Modules.Fixtures.Domain.Jogadores;
using Oddify.Modules.Fixtures.Domain.Ligas;
using Oddify.Modules.Fixtures.Domain.Partidas;
using Oddify.Modules.Fixtures.Infrastructure.Cotacoes;
using Oddify.Modules.Fixtures.Infrastructure.Database;
using Oddify.Modules.Fixtures.Infrastructure.Equipes;
using Oddify.Modules.Fixtures.Infrastructure.Escalacoes;
using Oddify.Modules.Fixtures.Infrastructure.EscalacoesDeJogador;
using Oddify.Modules.Fixtures.Infrastructure.EstatisticasDeEquipe;
using Oddify.Modules.Fixtures.Infrastructure.EstatisticasDeJogador;
using Oddify.Modules.Fixtures.Infrastructure.ExternalData;
using Oddify.Modules.Fixtures.Infrastructure.HealthChecks;
using Oddify.Modules.Fixtures.Infrastructure.Jogadores;
using Oddify.Modules.Fixtures.Infrastructure.Ligas;
using Oddify.Modules.Fixtures.Infrastructure.Partidas;
using Oddify.Modules.Fixtures.Infrastructure.PublicApi;
using Oddify.Modules.Fixtures.Infrastructure.Sincronizacao;
using Oddify.Modules.Fixtures.PublicApi;

namespace Oddify.Modules.Fixtures.Infrastructure;

public static class FixturesModule
{
    public static IServiceCollection AddFixturesModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEndpoints(Presentation.AssemblyReference.Assembly);
        services.AddInfrastructure(configuration);
        return services;
    }

    private static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        string databaseConnectionString = configuration.GetConnectionString("Database")!;

        services.AddDbContext<FixturesDbContext>((sp, options) =>
            options
                .UseNpgsql(databaseConnectionString, npgsqlOptions => npgsqlOptions
                    .MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Fixtures)
                    .EnableRetryOnFailure())
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(sp.GetRequiredService<InsertOutboxMessagesInterceptor>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<FixturesDbContext>());

        services.AddOutboxWriter<FixturesDbContext>();

        services.AddScoped<ILigaConfiguradaRepository, LigaConfiguradaRepository>();
        services.AddScoped<IEquipeRepository, EquipeRepository>();
        services.AddScoped<IJogadorRepository, JogadorRepository>();
        services.AddScoped<IPartidaRepository, PartidaRepository>();
        services.AddScoped<IEstatisticaEquipeRepository, EstatisticaEquipeRepository>();
        services.AddScoped<IEstatisticaJogadorRepository, EstatisticaJogadorRepository>();
        services.AddScoped<ICotacaoRepository, CotacaoRepository>();
        services.AddScoped<IEscalacaoRepository, EscalacaoRepository>();
        services.AddScoped<IEscalacaoJogadorRepository, EscalacaoJogadorRepository>();

        services.AddScoped<IFixturesApi, FixturesApi>();

        services.Configure<SincronizacaoExternaOptions>(configuration.GetSection("Fixtures:SincronizacaoExterna"));

        services.TryAddSingleton<OrcamentoDeRequisicoesApiFootball>();

        services.AddHttpClient<IApiFootballClient, ApiFootballClient>(client =>
        {
            client.BaseAddress = new Uri("https://v3.football.api-sports.io/");
            client.DefaultRequestHeaders.Add("x-apisports-key", Environment.GetEnvironmentVariable("APIFOOTBALL_API_KEY") ?? string.Empty);
        }).AddStandardResilienceHandler();

        services.AddHttpClient<ITheOddsApiClient, TheOddsApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.the-odds-api.com/v4/");
        }).AddStandardResilienceHandler();

        services.AddHostedService<SincronizacaoExternaBackgroundService>();

        services.AddHealthChecks()
            .AddCheck<ApiFootballHealthCheck>("api-football")
            .AddCheck<TheOddsApiHealthCheck>("the-odds-api");

        services.AddOutboxProcessor(Schemas.Fixtures);
    }
}
