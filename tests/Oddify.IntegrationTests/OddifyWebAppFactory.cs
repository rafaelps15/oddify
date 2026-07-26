using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;

namespace Oddify.IntegrationTests;

// Pública: xUnit descobre classes de teste (que recebem esta fábrica via IClassFixture/ICollectionFixture)
// apenas entre os tipos públicos do assembly.
#pragma warning disable CA1515
public sealed class OddifyWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("oddify")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private Respawner? _respawner;
    private string _connectionString = string.Empty;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _connectionString = _postgres.GetConnectionString();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = _connectionString,
                ["ConnectionStrings:Cache"] = "localhost:0",
            });
        });
    }

    public async Task ResetDatabaseAsync()
    {
        _respawner ??= await CriarRespawnerAsync();

        await using NpgsqlConnection connection = await AbrirConexaoAsync();
        await _respawner.ResetAsync(connection);
    }

    private async Task<Respawner> CriarRespawnerAsync()
    {
        await using NpgsqlConnection connection = await AbrirConexaoAsync();

        return await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            SchemasToInclude = ["fixtures", "analise", "apostas"],
            DbAdapter = DbAdapter.Postgres,
        });
    }

    private async Task<NpgsqlConnection> AbrirConexaoAsync()
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }
}
#pragma warning restore CA1515
