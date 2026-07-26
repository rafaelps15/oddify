using Microsoft.EntityFrameworkCore;
using Oddify.Modules.Analise.Infrastructure.Database;
using Oddify.Modules.Apostas.Infrastructure.Database;
using Oddify.Modules.Fixtures.Infrastructure.Database;
using Oddify.Modules.Users.Infrastructure.Database;

namespace Oddify.Api.Extensions;

internal static class MigrationExtensions
{
    internal static void ApplyMigrations(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();

        ApplyMigration<FixturesDbContext>(scope);
        ApplyMigration<AnaliseDbContext>(scope);
        ApplyMigration<ApostasDbContext>(scope);
        ApplyMigration<UsersDbContext>(scope);
    }

    private static void ApplyMigration<TDbContext>(IServiceScope scope)
        where TDbContext : DbContext
    {
        using TDbContext context = scope.ServiceProvider.GetRequiredService<TDbContext>();

        context.Database.Migrate();
    }
}
