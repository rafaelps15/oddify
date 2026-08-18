using Oddify.Common.Application.Authentication;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.Abstractions.Data;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.LiquidarMultipla;

internal sealed class LiquidarMultiplaCommandHandler(
    IApostaMultiplaRepository apostaMultiplaRepository,
    ApostaMultiplaLiquidacaoService liquidacaoService,
    IUnitOfWork unitOfWork,
    IUserContext userContext)
    : ICommandHandler<LiquidarMultiplaCommand>
{
    public async Task<Result> Handle(LiquidarMultiplaCommand request, CancellationToken cancellationToken)
    {
        ApostaMultipla? apostaMultipla = await apostaMultiplaRepository.GetAsync(request.ApostaMultiplaId, userContext.UserId, cancellationToken);
        if (apostaMultipla is null)
        {
            return Result.Failure(ApostaMultiplaErrors.NotFound(request.ApostaMultiplaId));
        }

        Result liquidarResult = await liquidacaoService.LiquidarAsync(apostaMultipla, cancellationToken);
        if (liquidarResult.IsFailure)
        {
            return liquidarResult;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
