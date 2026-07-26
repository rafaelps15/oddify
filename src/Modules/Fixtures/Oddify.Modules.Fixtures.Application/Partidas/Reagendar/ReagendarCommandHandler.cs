using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Application.Abstractions.Data;
using Oddify.Modules.Fixtures.Domain.Partidas;

namespace Oddify.Modules.Fixtures.Application.Partidas.Reagendar;

internal sealed class ReagendarCommandHandler(IPartidaRepository partidaRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<ReagendarCommand>
{
    public async Task<Result> Handle(ReagendarCommand request, CancellationToken cancellationToken)
    {
        Partida? partida = await partidaRepository.GetAsync(request.PartidaId, cancellationToken);

        if (partida is null)
        {
            return Result.Failure(PartidaErrors.NotFound(request.PartidaId));
        }

        Result result = partida.Reagendar(request.NovaDataUtc);

        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
