using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Analise.Application.Abstractions.Data;
using Oddify.Modules.Analise.Domain.Fixtures;

namespace Oddify.Modules.Analise.Application.Fixtures.RegistrarPartida;

// Idempotente: PartidaAgendadaIntegrationEvent pode, em tese, ser reprocessado (at-least-once) —
// se a partida espelhada já existe, não faz nada em vez de falhar ou duplicar.
internal sealed class RegistrarPartidaCommandHandler(IPartidaRepository partidaRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarPartidaCommand>
{
    public async Task<Result> Handle(RegistrarPartidaCommand request, CancellationToken cancellationToken)
    {
        Partida? partidaExistente = await partidaRepository.GetAsync(request.PartidaId, cancellationToken);
        if (partidaExistente is not null)
        {
            return Result.Success();
        }

        partidaRepository.Insert(
            Partida.Create(request.PartidaId, request.LigaId, request.EquipeCasaId, request.EquipeVisitanteId, request.DataUtc));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
