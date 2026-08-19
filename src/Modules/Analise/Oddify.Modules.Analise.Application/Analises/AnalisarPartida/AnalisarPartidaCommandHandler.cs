using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Analise.Application.Abstractions.Data;
using Oddify.Modules.Analise.Application.Calculo;
using Oddify.Modules.Analise.Domain.Analises;
using Oddify.Modules.Fixtures.PublicApi;

namespace Oddify.Modules.Analise.Application.Analises.AnalisarPartida;

internal sealed class AnalisarPartidaCommandHandler(
    IFixturesApi fixturesApi,
    IAnaliseDePartidaRepository analiseRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AnalisarPartidaCommand, Guid>
{
    private const int JanelaDeJogosRecentes = 10;

    public async Task<Result<Guid>> Handle(AnalisarPartidaCommand request, CancellationToken cancellationToken)
    {
        PartidaResponse? partida = await fixturesApi.ObterPartidaAsync(request.PartidaId, cancellationToken);
        if (partida is null)
        {
            return Result.Failure<Guid>(AnaliseDePartidaErrors.DadosIndisponiveis(request.PartidaId));
        }

        LigaResponse? liga = await fixturesApi.ObterLigaAsync(partida.LigaId, cancellationToken);
        HistoricoDeEquipeResponse? historicoCasa = await fixturesApi.ObterHistoricoRecenteAsync(partida.EquipeCasaId, JanelaDeJogosRecentes, cancellationToken);
        HistoricoDeEquipeResponse? historicoVisitante = await fixturesApi.ObterHistoricoRecenteAsync(partida.EquipeVisitanteId, JanelaDeJogosRecentes, cancellationToken);
        CotacaoResponse? cotacao = await fixturesApi.ObterCotacaoMaisRecenteAsync(request.PartidaId, request.Mercado, cancellationToken);

        if (liga is null || historicoCasa is null || historicoVisitante is null || cotacao is null)
        {
            return Result.Failure<Guid>(AnaliseDePartidaErrors.DadosIndisponiveis(request.PartidaId));
        }

        IReadOnlyCollection<CotacaoResponse> cotacoes = await fixturesApi.ObterCotacoesPorPartidaAsync(request.PartidaId, cancellationToken);
        IReadOnlyCollection<string> grupoDeMercados = MercadoResolver.ObterGrupoDeMercados(request.Mercado);

        var oddsDoGrupoDeMercado = new Dictionary<string, decimal>();

        // cotacoes ja vem ordenada por coletada_em_utc desc (GetCotacoesPorPartidaQueryHandler) — a primeira
        // ocorrencia de cada mercado é a mais recente, por isso TryAdd (nunca sobrescreve) é suficiente aqui.
        foreach (CotacaoResponse cotacaoDoGrupo in cotacoes.Where(c => c.Casa == cotacao.Casa && grupoDeMercados.Contains(c.Mercado)))
        {
            oddsDoGrupoDeMercado.TryAdd(cotacaoDoGrupo.Mercado, cotacaoDoGrupo.Odd);
        }

        if (!RemovedorDeMargem.GrupoCompleto(request.Mercado, oddsDoGrupoDeMercado))
        {
            // Cobertura incompleta da casa para as demais saídas do mercado (ex.: só over sem under) —
            // não dá para remover a margem com segurança, então a partida fica sem análise desta vez.
            return Result.Failure<Guid>(AnaliseDePartidaErrors.DadosIndisponiveis(request.PartidaId));
        }

        AnaliseCalculada calculo = AnaliseDePartidaCalculator.Calcular(
            liga, historicoCasa, historicoVisitante, cotacao, oddsDoGrupoDeMercado, request.Mercado);

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
}
