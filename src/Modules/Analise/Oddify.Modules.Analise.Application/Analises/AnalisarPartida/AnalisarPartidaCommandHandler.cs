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
        Result<PartidaResponse> partidaResult = await fixturesApi.ObterPartidaAsync(request.PartidaId, cancellationToken);
        if (partidaResult.IsFailure)
        {
            return Result.Failure<Guid>(partidaResult.Error);
        }

        PartidaResponse partida = partidaResult.Value;

        Result<LigaResponse> ligaResult = await fixturesApi.ObterLigaAsync(partida.LigaId, cancellationToken);
        if (ligaResult.IsFailure)
        {
            return Result.Failure<Guid>(ligaResult.Error);
        }

        LigaResponse liga = ligaResult.Value;

        Result<HistoricoDeEquipeResponse> historicoCasaResult =
            await fixturesApi.ObterHistoricoRecenteAsync(partida.EquipeCasaId, JanelaDeJogosRecentes, cancellationToken);
        if (historicoCasaResult.IsFailure)
        {
            return Result.Failure<Guid>(historicoCasaResult.Error);
        }

        HistoricoDeEquipeResponse historicoCasa = historicoCasaResult.Value;

        Result<HistoricoDeEquipeResponse> historicoVisitanteResult =
            await fixturesApi.ObterHistoricoRecenteAsync(partida.EquipeVisitanteId, JanelaDeJogosRecentes, cancellationToken);
        if (historicoVisitanteResult.IsFailure)
        {
            return Result.Failure<Guid>(historicoVisitanteResult.Error);
        }

        HistoricoDeEquipeResponse historicoVisitante = historicoVisitanteResult.Value;

        Result<CotacaoResponse> cotacaoResult =
            await fixturesApi.ObterCotacaoMaisRecenteAsync(request.PartidaId, request.Mercado, cancellationToken);
        if (cotacaoResult.IsFailure)
        {
            return Result.Failure<Guid>(cotacaoResult.Error);
        }

        CotacaoResponse cotacao = cotacaoResult.Value;

        (decimal lambdaCasa, decimal lambdaVisitante) = PoissonCalculator.CalcularLambdas(
            liga.MediaDeGols,
            liga.FatorCasa,
            historicoCasa.MediaGolsFeitos,
            historicoCasa.MediaGolsSofridos,
            historicoVisitante.MediaGolsFeitos,
            historicoVisitante.MediaGolsSofridos);

        decimal[,] matrizPura = PoissonCalculator.MatrizDePlacares(lambdaCasa, lambdaVisitante);
        decimal probPoissonPura = ProbabilidadeDeMercadoCalculator.Calcular(matrizPura, request.Mercado);

        decimal[,] matrizCorrigida = DixonColesCorrecao.Aplicar(matrizPura, lambdaCasa, lambdaVisitante);
        decimal probDixonColes = ProbabilidadeDeMercadoCalculator.Calcular(matrizCorrigida, request.Mercado);

        decimal probImplicitaDaOdd = 1m / cotacao.Odd;
        decimal vantagem = probDixonColes - probImplicitaDaOdd;
        int amostraMinima = Math.Min(historicoCasa.AmostraDeJogos, historicoVisitante.AmostraDeJogos);

        (bool aprovada, string? motivo) = FiltroDeOportunidades.Avaliar(vantagem, cotacao.Odd, amostraMinima, liga.Calibrada);

        var analise = AnaliseDePartida.Create(
            request.PartidaId,
            request.Mercado,
            probPoissonPura,
            probDixonColes,
            probImplicitaDaOdd,
            vantagem,
            cotacao.Odd,
            aprovada,
            motivo,
            DateTime.UtcNow);

        analiseRepository.Insert(analise);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return analise.Id;
    }
}
