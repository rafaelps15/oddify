using Microsoft.EntityFrameworkCore;
using Oddify.Common.Application.Authentication;
using Oddify.Common.Application.Clock;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.Abstractions.Data;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;
using Oddify.Modules.Apostas.Domain.Bancas;
using Oddify.Modules.Apostas.Domain.MovimentacoesDaBanca;
using Oddify.Modules.Apostas.Domain.PernasDeAposta;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.EstornarLiquidacaoMultipla;

internal sealed class EstornarLiquidacaoMultiplaCommandHandler(
    IApostaMultiplaRepository apostaMultiplaRepository,
    IPernaDeApostaRepository pernaDeApostaRepository,
    IBancaRepository bancaRepository,
    IMovimentacaoDaBancaRepository movimentacaoDaBancaRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : ICommandHandler<EstornarLiquidacaoMultiplaCommand>
{
    public async Task<Result> Handle(EstornarLiquidacaoMultiplaCommand request, CancellationToken cancellationToken)
    {
        ApostaMultipla? apostaMultipla = await apostaMultiplaRepository.GetAsync(request.ApostaMultiplaId, userContext.UserId, cancellationToken);
        if (apostaMultipla is null)
        {
            return Result.Failure(ApostaMultiplaErrors.NotFound(request.ApostaMultiplaId));
        }

        DateTime agora = dateTimeProvider.UtcNow;

        Result<decimal> estornarResult = apostaMultipla.Estornar(agora);
        if (estornarResult.IsFailure)
        {
            return Result.Failure(estornarResult.Error);
        }

        IReadOnlyCollection<PernaDeAposta> pernas = await pernaDeApostaRepository.GetPorApostaMultiplaAsync(request.ApostaMultiplaId, cancellationToken);

        foreach (PernaDeAposta perna in pernas)
        {
            Result reabrirResult = perna.Reabrir();
            if (reabrirResult.IsFailure)
            {
                return reabrirResult;
            }
        }

        Banca? banca = await bancaRepository.GetAsync(apostaMultipla.BancaId, userContext.UserId, cancellationToken);
        if (banca is null)
        {
            return Result.Failure(BancaErrors.NotFound(apostaMultipla.BancaId));
        }

        decimal reversao = -estornarResult.Value;

        MovimentacaoDaBanca movimentacao = banca.RegistrarMovimentacao(reversao, TipoDeMovimentacao.Estorno, apostaMultipla.Id, agora);
        movimentacaoDaBancaRepository.Insert(movimentacao);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Ver comentário equivalente em LiquidarMultiplaCommandHandler.
            return Result.Failure(CommonErrors.ConflitoDeConcorrencia);
        }

        return Result.Success();
    }
}
