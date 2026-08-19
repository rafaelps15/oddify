using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Analise.Application.Abstractions.Data;
using Oddify.Modules.Analise.Application.Calculo;
using Oddify.Modules.Analise.Domain.Analises;
using Oddify.Modules.Analise.Domain.Fixtures;

namespace Oddify.Modules.Analise.Application.Analises.AnalisarPartida;

internal sealed class AnalisarPartidaCommandHandler(
    ILigaRepository ligaRepository,
    IPartidaRepository partidaRepository,
    ICotacaoRepository cotacaoRepository,
    IAnaliseDePartidaRepository analiseRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AnalisarPartidaCommand, Guid>
{
    private const int JanelaDeJogosRecentes = 10;

    public async Task<Result<Guid>> Handle(AnalisarPartidaCommand request, CancellationToken cancellationToken)
    {
        AnaliseCalculada? calculo = await ObterCalculoAsync(request.PartidaId, request.Mercado, cancellationToken);

        if (calculo is null)
        {
            return Result.Failure<Guid>(AnaliseDePartidaErrors.DadosIndisponiveis(request.PartidaId));
        }

        var analise = AnaliseDePartida.Create(
            request.PartidaId,
            request.Mercado,
            calculo.ProbPoissonPura,
            calculo.ProbDixonColes,
            calculo.ProbImplicitaDaOdd,
            calculo.Vantagem,
            calculo.Odd,
            calculo.Aprovada,
            calculo.Motivo,
            DateTime.UtcNow);

        analiseRepository.Insert(analise);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return analise.Id;
    }

    private async Task<AnaliseCalculada?> ObterCalculoAsync(Guid partidaId, string mercado, CancellationToken cancellationToken)
    {
        Partida? partida = await partidaRepository.GetAsync(partidaId, cancellationToken);
        if (partida is null)
        {
            return null;
        }

        Liga? liga = await ligaRepository.GetAsync(partida.LigaId, cancellationToken);
        if (liga is null)
        {
            return null;
        }

        HistoricoDeEquipe historicoCasa = HistoricoDeEquipeCalculator.Calcular(
            await partidaRepository.GetRecentesPorEquipeAsync(partida.EquipeCasaId, JanelaDeJogosRecentes, cancellationToken),
            partida.EquipeCasaId);

        HistoricoDeEquipe historicoVisitante = HistoricoDeEquipeCalculator.Calcular(
            await partidaRepository.GetRecentesPorEquipeAsync(partida.EquipeVisitanteId, JanelaDeJogosRecentes, cancellationToken),
            partida.EquipeVisitanteId);

        Cotacao? cotacao = await cotacaoRepository.GetMaisRecenteAsync(partidaId, mercado, cancellationToken);
        if (cotacao is null)
        {
            return null;
        }

        IReadOnlyDictionary<string, decimal> oddsDoGrupoDeMercado =
            await ObterOddsDoGrupoNaMesmaCasaAsync(partidaId, mercado, cotacao.Casa, cancellationToken);

        if (!RemovedorDeMargem.GrupoCompleto(mercado, oddsDoGrupoDeMercado))
        {
            // Cobertura incompleta da casa para as demais saídas do mercado (ex.: só over sem under) —
            // não dá para remover a margem com segurança, então a partida fica sem análise desta vez.
            return null;
        }

        return AnaliseDePartidaCalculator.Calcular(liga, historicoCasa, historicoVisitante, cotacao, oddsDoGrupoDeMercado, mercado);
    }

    private async Task<IReadOnlyDictionary<string, decimal>> ObterOddsDoGrupoNaMesmaCasaAsync(
        Guid partidaId,
        string mercado,
        string casa,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Cotacao> cotacoes = await cotacaoRepository.GetPorPartidaAsync(partidaId, cancellationToken);
        IReadOnlyCollection<string> grupo = MercadoResolver.ObterGrupoDeMercados(mercado);

        var oddsPorMercado = new Dictionary<string, decimal>();

        // cotacoes ja vem ordenada por ColetadaEmUtc desc (ICotacaoRepository.GetPorPartidaAsync) — a
        // primeira ocorrencia de cada mercado é a mais recente, por isso TryAdd (nunca sobrescreve) é
        // suficiente aqui.
        foreach (Cotacao cotacaoDoGrupo in cotacoes.Where(c => c.Casa == casa && grupo.Contains(c.Mercado)))
        {
            oddsPorMercado.TryAdd(cotacaoDoGrupo.Mercado, cotacaoDoGrupo.Odd);
        }

        return oddsPorMercado;
    }
}
