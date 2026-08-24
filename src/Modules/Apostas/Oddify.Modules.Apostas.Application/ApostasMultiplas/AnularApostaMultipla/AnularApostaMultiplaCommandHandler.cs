using Microsoft.EntityFrameworkCore;
using Oddify.Common.Application.Authentication;
using Oddify.Common.Application.Clock;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.Abstractions.Data;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;
using Oddify.Modules.Apostas.Domain.PernasDeAposta;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.AnularApostaMultipla;

internal sealed class AnularApostaMultiplaCommandHandler(
    IApostaMultiplaRepository apostaMultiplaRepository,
    IPernaDeApostaRepository pernaDeApostaRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : ICommandHandler<AnularApostaMultiplaCommand>
{
    public async Task<Result> Handle(AnularApostaMultiplaCommand request, CancellationToken cancellationToken)
    {
        ApostaMultipla? apostaMultipla = await apostaMultiplaRepository.GetAsync(request.ApostaMultiplaId, userContext.UserId, cancellationToken);
        if (apostaMultipla is null)
        {
            return Result.Failure(ApostaMultiplaErrors.NotFound(request.ApostaMultiplaId));
        }

        DateTime agora = dateTimeProvider.UtcNow;

        Result anularResult = apostaMultipla.Anular(agora);
        if (anularResult.IsFailure)
        {
            return anularResult;
        }

        IReadOnlyCollection<PernaDeAposta> pernas = await pernaDeApostaRepository.GetPorApostaMultiplaAsync(request.ApostaMultiplaId, cancellationToken);

        foreach (PernaDeAposta perna in pernas)
        {
            Result anularPernaResult = perna.Anular();
            if (anularPernaResult.IsFailure)
            {
                return anularPernaResult;
            }
        }

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
