using Anthropic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Oddify.Common.Infrastructure.Interceptors;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Modules.Analise.Application.Abstractions.Data;
using Oddify.Modules.Analise.Application.Abstractions.Llm;
using Oddify.Modules.Analise.Domain.Analises;
using Oddify.Modules.Analise.Infrastructure.Analises;
using Oddify.Modules.Analise.Infrastructure.Database;
using Oddify.Modules.Analise.Infrastructure.Llm;
using Oddify.Modules.Analise.Infrastructure.PublicApi;
using Oddify.Modules.Analise.PublicApi;

namespace Oddify.Modules.Analise.Infrastructure;

public static class AnaliseModule
{
    public static IServiceCollection AddAnaliseModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEndpoints(Presentation.AssemblyReference.Assembly);
        services.AddInfrastructure(configuration);
        return services;
    }

    private static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        string databaseConnectionString = configuration.GetConnectionString("Database")!;

        services.AddDbContext<AnaliseDbContext>((sp, options) =>
            options
                .UseNpgsql(databaseConnectionString, npgsqlOptions => npgsqlOptions
                    .MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Analise))
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(sp.GetRequiredService<PublishDomainEventsInterceptor>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AnaliseDbContext>());

        services.AddScoped<IAnaliseDePartidaRepository, AnaliseDePartidaRepository>();

        services.AddSingleton<IAnaliseApi, AnaliseApi>();

        services.AddSingleton<AnthropicClient>(_ => new AnthropicClient());
        services.AddScoped<IClaudeAvaliadorCriticoService, ClaudeAvaliadorCriticoService>();
    }
}
