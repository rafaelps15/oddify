using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Oddify.Common.Application.Authorization;
using Oddify.Common.Application.Caching;
using Oddify.Common.Application.EventBus;
using Oddify.Common.Infrastructure.Authorization;
using Oddify.Common.Infrastructure.Inbox;
using Oddify.Common.Infrastructure.Outbox;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Modules.Users.Application.Abstractions.Data;
using Oddify.Modules.Users.Application.Abstractions.EmailVerification;
using Oddify.Modules.Users.Application.Abstractions.PasswordReset;
using Oddify.Modules.Users.Application.Users.EmailVerification;
using Oddify.Modules.Users.Application.Users.PasswordReset;
using Oddify.Modules.Users.Domain.EmailVerification;
using Oddify.Modules.Users.Domain.PasswordReset;
using Oddify.Modules.Users.Domain.Roles;
using Oddify.Modules.Users.Domain.Users;
using Oddify.Modules.Users.Infrastructure.Authorization;
using Oddify.Modules.Users.Infrastructure.Database;
using Oddify.Modules.Users.Infrastructure.EmailVerification;
using Oddify.Modules.Users.Infrastructure.Inbox;
using Oddify.Modules.Users.Infrastructure.PasswordReset;
using Oddify.Modules.Users.Infrastructure.Roles;
using Oddify.Modules.Users.Infrastructure.Users;
using Presentation = Oddify.Modules.Users.Presentation;

namespace Oddify.Modules.Users.Infrastructure;

public static class UsersModule
{
    public static IServiceCollection AddUsersModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEndpoints(Presentation.AssemblyReference.Assembly);
        services.AddIntegrationEventHandlers(Presentation.AssemblyReference.Assembly, Schemas.Users);
        services.AddInfrastructure(configuration);
        return services;
    }

    // Um IntegrationEventConsumer<T> (Infrastructure/Inbox) por tipo de integration event que
    // este módulo consome — ver comentário equivalente em ApostasModule.
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

        services.AddDbContext<UsersDbContext>((sp, options) =>
            options
                .UseNpgsql(databaseConnectionString, npgsqlOptions => npgsqlOptions
                    .MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Users)
                    .EnableRetryOnFailure())
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(sp.GetRequiredService<InsertOutboxMessagesInterceptor>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<UsersDbContext>());

        services.AddScoped<IUserRepository, UserRepository>();

        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        services.AddScoped<IRoleRepository, RoleRepository>();

        services.AddScoped<IUserRoleRepository, UserRoleRepository>();

        services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>();
        services.AddScoped<EmailVerificationTokenIssuer>();

        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<PasswordResetTokenIssuer>();

        services.AddOutboxWriter<UsersDbContext>();

        services.Configure<EmailVerificationOptions>(configuration.GetSection("Users:EmailVerification"));
        services.Configure<PasswordResetOptions>(configuration.GetSection("Users:PasswordReset"));

        // PermissionProvider é registrado em Common.Infrastructure/InfrastructureConfiguration.cs —
        // vive lá porque PermissionAuthorizationHandler (autorização cross-cutting) o resolve
        // diretamente, sem depender de nenhum módulo específico.
        services.AddScoped<IPermissionService>(sp =>
            new CachedPermissionService(sp.GetRequiredService<PermissionProvider>(), sp.GetRequiredService<ICacheService>()));

        services.AddOutboxProcessor(Schemas.Users);
        services.AddInboxProcessor(Schemas.Users, Presentation.AssemblyReference.Assembly);
    }
}
