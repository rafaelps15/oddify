using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Oddify.Modules.Users.Infrastructure.Database;

public sealed class UsersDbContextFactory : IDesignTimeDbContextFactory<UsersDbContext>
{
    public UsersDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<UsersDbContext>();

        // Só usada pelo `dotnet ef migrations` em tempo de design (nunca em runtime da app) — credencial
        // padrão do Postgres local do docker-compose, não um segredo real.
#pragma warning disable S2068
        const string designTimeConnectionString = "Host=localhost;Port=5433;Database=oddify;Username=postgres;Password=postgres";
#pragma warning restore S2068

        optionsBuilder
            .UseNpgsql(
                designTimeConnectionString,
                npgsqlOptions => npgsqlOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Users))
            .UseSnakeCaseNamingConvention();

        return new UsersDbContext(optionsBuilder.Options);
    }
}
