using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Oddify.Common.Application.Authorization;
using Oddify.Common.Application.Caching;
using Oddify.Common.Infrastructure.Authorization;
using Oddify.Common.Infrastructure.Interceptors;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Modules.Users.Application.Abstractions.Data;
using Oddify.Modules.Users.Application.Abstractions.EmailVerification;
using Oddify.Modules.Users.Application.Abstractions.Outbox;
using Oddify.Modules.Users.Application.Users.EmailVerification;
using Oddify.Modules.Users.Domain.EmailVerification;
using Oddify.Modules.Users.Domain.Roles;
using Oddify.Modules.Users.Domain.Users;
using Oddify.Modules.Users.Infrastructure.Authorization;
using Oddify.Modules.Users.Infrastructure.Database;
using Oddify.Modules.Users.Infrastructure.EmailVerification;
using Oddify.Modules.Users.Infrastructure.Outbox;
using Oddify.Modules.Users.Infrastructure.Roles;
using Oddify.Modules.Users.Infrastructure.Users;
using Oddify.Modules.Users.Presentation.IntegrationEvents;
using Quartz;

namespace Oddify.Modules.Users.Infrastructure;

public static class UsersModule
{
    public static IServiceCollection AddUsersModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEndpoints(Presentation.AssemblyReference.Assembly);
        services.AddInfrastructure(configuration);
        return services;
    }

    public static void ConfigureConsumers(IRegistrationConfigurator registrationConfigurator)
    {
        registrationConfigurator.AddConsumer<SendVerificationEmailIntegrationEventConsumer>();
        registrationConfigurator.AddConsumer<SendWelcomeEmailIntegrationEventConsumer>();
    }

    private static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        string databaseConnectionString = configuration.GetConnectionString("Database")!;

        // De volta ao PublishDomainEventsInterceptor padrão (o mesmo dos outros módulos) — a
        // outbox agora só guarda o que vai virar mensagem no bus (IOutboxWriter, chamado
        // explicitamente pelos handlers síncronos), sem captura automática de domain events.
        services.AddDbContext<UsersDbContext>((sp, options) =>
            options
                .UseNpgsql(databaseConnectionString, npgsqlOptions => npgsqlOptions
                    .MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Users)
                    .EnableRetryOnFailure())
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(sp.GetRequiredService<PublishDomainEventsInterceptor>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<UsersDbContext>());

        services.AddScoped<IUserRepository, UserRepository>();

        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        services.AddScoped<IRoleRepository, RoleRepository>();

        services.AddScoped<IUserRoleRepository, UserRoleRepository>();

        services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>();
        services.AddScoped<EmailVerificationTokenIssuer>();

        services.AddScoped<IOutboxWriter, OutboxWriter>();

        services.Configure<EmailVerificationOptions>(configuration.GetSection("Users:EmailVerification"));

        services.AddOutboxProcessor(configuration);

        // PermissionProvider é registrado em Common.Infrastructure/InfrastructureConfiguration.cs —
        // vive lá porque PermissionAuthorizationHandler (autorização cross-cutting) o resolve
        // diretamente, sem depender de nenhum módulo específico.
        services.AddScoped<IPermissionService>(sp =>
            new CachedPermissionService(sp.GetRequiredService<PermissionProvider>(), sp.GetRequiredService<ICacheService>()));
    }

    private static void AddOutboxProcessor(this IServiceCollection services, IConfiguration configuration)
    {
        OutboxProcessorOptions options =
            configuration.GetSection("Users:OutboxProcessor").Get<OutboxProcessorOptions>() ?? new OutboxProcessorOptions();

        services.Configure<OutboxProcessorOptions>(configuration.GetSection("Users:OutboxProcessor"));
        services.AddHostedService<OutboxCleanupBackgroundService>();

        if (!options.Enabled)
        {
            return;
        }

        services.AddQuartz(quartz =>
        {
            var jobKey = new JobKey(nameof(OutboxProcessorJob));

            quartz.AddJob<OutboxProcessorJob>(job => job.WithIdentity(jobKey));

            quartz.AddTrigger(trigger => trigger
                .ForJob(jobKey)
                .WithSimpleSchedule(schedule => schedule.WithInterval(options.Interval).RepeatForever()));
        });

        services.AddQuartzHostedService(quartz => quartz.WaitForJobsToComplete = true);
    }
}
