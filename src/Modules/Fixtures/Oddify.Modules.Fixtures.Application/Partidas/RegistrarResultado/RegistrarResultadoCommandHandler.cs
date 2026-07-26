using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Application.Abstractions.Data;
using Oddify.Modules.Fixtures.Domain.Partidas;

namespace Oddify.Modules.Fixtures.Application.Partidas.RegistrarResultado;

internal sealed class RegistrarResultadoCommandHandler(IPartidaRepository partidaRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarResultadoCommand>
{
    public async Task<Result> Handle(RegistrarResultadoCommand request, CancellationToken cancellationToken)
    {
        Partida? partida = await partidaRepository.GetAsync(request.PartidaId, cancellationToken);

        if (partida is null)
        {
            return Result.Failure(PartidaErrors.NotFound(request.PartidaId));
        }

        Result result = partida.RegistrarResultado(request.GolsCasa, request.GolsVisitante);

        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
