using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Analise.Application.Abstractions.Data;
using Oddify.Modules.Analise.Domain.Fixtures;

namespace Oddify.Modules.Analise.Application.Fixtures.RegistrarResultadoDaPartida;

internal sealed class RegistrarResultadoDaPartidaCommandHandler(IPartidaRepository partidaRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarResultadoDaPartidaCommand>
{
    public async Task<Result> Handle(RegistrarResultadoDaPartidaCommand request, CancellationToken cancellationToken)
    {
        Partida? partida = await partidaRepository.GetAsync(request.PartidaId, cancellationToken);
        if (partida is null)
        {
            return Result.Failure(PartidaErrors.NotFound(request.PartidaId));
        }

        partida.RegistrarResultado(request.GolsCasa, request.GolsVisitante);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
