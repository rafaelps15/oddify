using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Analise.PublicApi;
using Oddify.Modules.Apostas.Application.Abstractions.Data;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;
using Oddify.Modules.Apostas.Domain.Bancas;
using Oddify.Modules.Apostas.Domain.PernasDeAposta;
using Oddify.Modules.Fixtures.PublicApi;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.LiquidarMultipla;

internal sealed class LiquidarMultiplaCommandHandler(
    IApostaMultiplaRepository apostaMultiplaRepository,
    IPernaDeApostaRepository pernaDeApostaRepository,
    IBancaRepository bancaRepository,
    IFixturesApi fixturesApi,
    IAnaliseApi analiseApi,
    IUnitOfWork unitOfWork)
    : ICommandHandler<LiquidarMultiplaCommand>
{
    public async Task<Result> Handle(LiquidarMultiplaCommand request, CancellationToken cancellationToken)
    {
        ApostaMultipla? apostaMultipla = await apostaMultiplaRepository.GetAsync(request.ApostaMultiplaId, cancellationToken);
        if (apostaMultipla is null)
        {
            return Result.Failure(ApostaMultiplaErrors.NotFound(request.ApostaMultiplaId));
        }

        IReadOnlyCollection<PernaDeAposta> pernas =
            await pernaDeApostaRepository.GetPorApostaMultiplaAsync(request.ApostaMultiplaId, cancellationToken);

        var resultadosDasPernas = new List<bool>();

        foreach (PernaDeAposta perna in pernas)
        {
            Result<PartidaResponse> partidaResult = await fixturesApi.ObterPartidaAsync(perna.PartidaId, cancellationToken);

            if (partidaResult.IsFailure)
            {
                return Result.Failure(partidaResult.Error);
            }

            PartidaResponse partida = partidaResult.Value;

            if (partida.GolsCasa is null || partida.GolsVisitante is null)
            {
                return Result.Failure(Error.Problem(
                    "ApostasMultiplas.PartidaNaoEncerrada",
                    $"A partida {perna.PartidaId} ainda não foi encerrada"));
            }

            bool ganhouPerna = analiseApi.ResolverMercado(perna.Mercado, partida.GolsCasa.Value, partida.GolsVisitante.Value);

            Result resolverResult = perna.Resolver(ganhouPerna);
            if (resolverResult.IsFailure)
            {
                return resolverResult;
            }

            resultadosDasPernas.Add(ganhouPerna);
        }

        bool multiplaGanhou = resultadosDasPernas.Count > 0 && resultadosDasPernas.TrueForAll(ganhou => ganhou);

        Result liquidarResult = apostaMultipla.Liquidar(multiplaGanhou);
        if (liquidarResult.IsFailure)
        {
            return liquidarResult;
        }

        Banca? banca = await bancaRepository.GetAsync(apostaMultipla.BancaId, cancellationToken);
        if (banca is null)
        {
            return Result.Failure(BancaErrors.NotFound(apostaMultipla.BancaId));
        }

        banca.AjustarSaldo(apostaMultipla.LucroOuPerda!.Value);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
