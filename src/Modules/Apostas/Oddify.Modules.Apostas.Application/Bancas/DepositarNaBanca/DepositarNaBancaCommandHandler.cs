using Oddify.Common.Application.Authentication;
using Oddify.Common.Application.Clock;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.Abstractions.Data;
using Oddify.Modules.Apostas.Domain.Bancas;
using Oddify.Modules.Apostas.Domain.MovimentacoesDaBanca;

namespace Oddify.Modules.Apostas.Application.Bancas.DepositarNaBanca;

internal sealed class DepositarNaBancaCommandHandler(
    IBancaRepository bancaRepository,
    IMovimentacaoDaBancaRepository movimentacaoDaBancaRepository,
    IUnitOfWork unitOfWork,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<DepositarNaBancaCommand>
{
    public async Task<Result> Handle(DepositarNaBancaCommand request, CancellationToken cancellationToken)
    {
        Banca? banca = await bancaRepository.GetAsync(request.BancaId, userContext.UserId, cancellationToken);
        if (banca is null)
        {
            return Result.Failure(BancaErrors.NotFound(request.BancaId));
        }

        DateTime agora = dateTimeProvider.UtcNow;

        banca.AjustarSaldo(request.Valor, agora);

        var movimentacao = MovimentacaoDaBanca.Create(
            banca.Id, TipoDeMovimentacao.Deposito, request.Valor, banca.SaldoAtual, apostaMultiplaId: null, agora);
        movimentacaoDaBancaRepository.Insert(movimentacao);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
