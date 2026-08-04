using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oddify.Modules.Fixtures.Application.Abstractions.ExternalData;
using Oddify.Modules.Fixtures.Application.Cotacoes.SincronizarCotacoes;
using Oddify.Modules.Fixtures.Application.Ligas.SincronizarFixturesDaLiga;
using Oddify.Modules.Fixtures.Domain.Ligas;

namespace Oddify.Modules.Fixtures.Infrastructure.Sincronizacao;

internal sealed class SincronizacaoExternaBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<SincronizacaoExternaOptions> opcoes,
    ILogger<SincronizacaoExternaBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!opcoes.Value.Habilitada)
        {
            logger.LogInformation("Sincronização externa de fixtures está desabilitada — job não será iniciado.");
            return;
        }

        using var timer = new PeriodicTimer(opcoes.Value.Intervalo);

        do
        {
            await SincronizarUmaVezAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task SincronizarUmaVezAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();

        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();
        ILigaConfiguradaRepository ligaRepository = scope.ServiceProvider.GetRequiredService<ILigaConfiguradaRepository>();

        IReadOnlyCollection<LigaConfigurada> ligas = await ligaRepository.ListarTodasAsync(cancellationToken);

        foreach (LigaConfigurada liga in ligas)
        {
            int temporada = opcoes.Value.ApiFootballTemporadas.GetValueOrDefault(liga.IdExterno, DateTime.UtcNow.Year);

            try
            {
                await sender.Send(new SincronizarFixturesDaLigaCommand(liga.Id, temporada), cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao sincronizar fixtures da liga {LigaId}", liga.Id);
            }
        }

        try
        {
            await sender.Send(new SincronizarCotacoesCommand(), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao sincronizar cotações");
        }
    }
}
