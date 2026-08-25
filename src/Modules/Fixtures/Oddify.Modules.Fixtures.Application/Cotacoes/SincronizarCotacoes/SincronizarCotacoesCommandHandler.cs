using Microsoft.Extensions.Options;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Application.Abstractions.Data;
using Oddify.Modules.Fixtures.Application.Abstractions.ExternalData;
using Oddify.Modules.Fixtures.Application.Calculo;
using Oddify.Modules.Fixtures.Domain.Cotacoes;
using Oddify.Modules.Fixtures.Domain.Equipes;
using Oddify.Modules.Fixtures.Domain.Ligas;
using Oddify.Modules.Fixtures.Domain.Partidas;

namespace Oddify.Modules.Fixtures.Application.Cotacoes.SincronizarCotacoes;

internal sealed class SincronizarCotacoesCommandHandler(
    IPartidaRepository partidaRepository,
    IEquipeRepository equipeRepository,
    ILigaConfiguradaRepository ligaRepository,
    ICotacaoRepository cotacaoRepository,
    ITheOddsApiClient theOddsApiClient,
    IOptions<SincronizacaoExternaOptions> opcoes,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SincronizarCotacoesCommand>
{
    private static readonly TimeSpan JanelaDeBusca = TimeSpan.FromHours(48);

    public async Task<Result> Handle(SincronizarCotacoesCommand request, CancellationToken cancellationToken)
    {
        DateTime agora = DateTime.UtcNow;

        IReadOnlyCollection<Partida> partidasProximas =
            await partidaRepository.ListarAgendadasEntreAsync(agora, agora + JanelaDeBusca, cancellationToken);

        foreach (IGrouping<Guid, Partida> grupo in partidasProximas.GroupBy(p => p.LigaId))
        {
            await CotacaoSincronizacaoFactory.SincronizarLigaAsync(
                grupo.Key, [.. grupo], ligaRepository, equipeRepository, cotacaoRepository, theOddsApiClient,
                opcoes.Value.TheOddsApiSportKeys, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
