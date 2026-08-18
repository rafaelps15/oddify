using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Npgsql;
using Oddify.Common.Application.Authentication;
using Oddify.Common.Application.Caching;
using Oddify.Common.Application.Clock;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Emailing;
using Oddify.Common.Application.EventBus;
using Oddify.Common.Application.Outbox;
using Oddify.Common.Infrastructure.Authentication;
using Oddify.Common.Infrastructure.Authorization;
using Oddify.Common.Infrastructure.Caching;
using Oddify.Common.Infrastructure.Clock;
using Oddify.Common.Infrastructure.Data;
using Oddify.Common.Infrastructure.Emailing;
using Oddify.Common.Infrastructure.Outbox;
using Quartz;
using StackExchange.Redis;

namespace Oddify.Common.Infrastructure;

public static class InfrastructureConfiguration
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IRegistrationConfigurator>[] moduleConfigureConsumers,
        string databaseConnectionString,
        string redisConnectionString)
    {
        NpgsqlDataSource npgsqlDataSource = new NpgsqlDataSourceBuilder(databaseConnectionString).Build();
        services.TryAddSingleton(npgsqlDataSource);

        services.TryAddScoped<IDbConnectionFactory, DbConnectionFactory>();

        services.TryAddSingleton<InsertOutboxMessagesInterceptor>();

        services.TryAddSingleton<IDateTimeProvider, DateTimeProvider>();

        try
        {
            IConnectionMultiplexer connectionMultiplexer = ConnectionMultiplexer.Connect(redisConnectionString);
            services.TryAddSingleton(connectionMultiplexer);

            services.AddStackExchangeRedisCache(options =>
                options.ConnectionMultiplexerFactory = () => Task.FromResult(connectionMultiplexer));
        }
        catch
        {
            services.AddDistributedMemoryCache();
        }

        services.TryAddSingleton<ICacheService, CacheService>();

        services.TryAddSingleton<IEventBus, EventBus.EventBus>();

        // Validação: [OptionsValidator] em ValidadorJwtOptions gera Validate(...) em tempo de
        // compilação a partir dos DataAnnotations de JwtOptions (sem reflexão, compatível com
        // AOT) — por isso não há .ValidateDataAnnotations() aqui, só .ValidateOnStart() pra
        // rodar o validador (registrado logo abaixo via IValidateOptions<JwtOptions>) no boot em
        // vez de na primeira leitura.
        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<JwtOptions>, ValidadorJwtOptions>();

        // A configuração de fato do JwtBearerOptions (chave, issuer, audience) fica encapsulada
        // em JwtBearerOptionsPostConfigure — resolvido via DI, lê o JwtOptions já validado em vez
        // de reler configuração crua dentro de um lambda capturando IServiceProvider.
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPostConfigureOptions<JwtBearerOptions>, JwtBearerOptionsPostConfigure>());

        services.AddAuthorization();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddScoped<PermissionProvider>();
        services.AddHttpContextAccessor();
        services.TryAddScoped<IUserContext, UserContext>();
        services.TryAddSingleton<IPasswordHasher, PasswordHasher>();
        services.TryAddSingleton<ITokenProvider, TokenProvider>();
        services.TryAddSingleton<IEmailSender, ConsoleEmailSender>();

        services.AddMassTransit(configure =>
        {
            foreach (Action<IRegistrationConfigurator> configureConsumer in moduleConfigureConsumers)
            {
                configureConsumer(configure);
            }

            configure.SetKebabCaseEndpointNameFormatter();

            configure.UsingInMemory((context, cfg) =>
            {
                cfg.ConfigureEndpoints(context);
            });
        });

        // Cada módulo contribui seu próprio job (AddOutboxProcessor/AddInboxProcessor, chamados de
        // dentro do próprio composition root) via IConfigureOptions<QuartzOptions> — aqui só o
        // bootstrapping compartilhado: opções, o scheduler em si, e o cleanup (que enumera
        // IEnumerable<OutboxModule>/IEnumerable<InboxModule> contribuído por cada módulo).
        services.Configure<OutboxProcessorOptions>(configuration.GetSection("OutboxProcessor"));
        services.AddQuartz();
        services.AddQuartzHostedService(quartz => quartz.WaitForJobsToComplete = true);
        services.AddHostedService<OutboxCleanupBackgroundService>();

        return services;
    }
}
