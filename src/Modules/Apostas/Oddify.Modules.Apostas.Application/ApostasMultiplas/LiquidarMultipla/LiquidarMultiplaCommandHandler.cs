using MediatR;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.Abstractions.Data;
using Oddify.Modules.Apostas.Application.ApostasMultiplas.GetResultadosDasPernas;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;
using Oddify.Modules.Apostas.Domain.Bancas;
using Oddify.Modules.Apostas.Domain.PernasDeAposta;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.LiquidarMultipla;

internal sealed class LiquidarMultiplaCommandHandler(
    IApostaMultiplaRepository apostaMultiplaRepository,
    IPernaDeApostaRepository pernaDeApostaRepository,
    IBancaRepository bancaRepository,
    ISender sender,
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

        var query = new GetResultadosDasPernasQuery(
            [.. pernas.Select(perna => new PernaParaResolver(perna.Id, perna.PartidaId, perna.Mercado))]);

        Result<IReadOnlyDictionary<Guid, bool>> resultados = await sender.Send(query, cancellationToken);
        if (resultados.IsFailure)
        {
            return Result.Failure(resultados.Error);
        }

        var resultadosDasPernas = new List<bool>();

        foreach (PernaDeAposta perna in pernas)
        {
            bool ganhouPerna = resultados.Value[perna.Id];

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
