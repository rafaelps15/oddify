using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Application.Abstractions.Data;
using Oddify.Modules.Fixtures.Domain.Equipes;
using Oddify.Modules.Fixtures.Domain.Ligas;
using Oddify.Modules.Fixtures.Domain.Partidas;

namespace Oddify.Modules.Fixtures.Application.Partidas.CriarPartida;

internal sealed class CriarPartidaCommandHandler(
    IPartidaRepository partidaRepository,
    ILigaConfiguradaRepository ligaRepository,
    IEquipeRepository equipeRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CriarPartidaCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CriarPartidaCommand request, CancellationToken cancellationToken)
    {
        if (request.EquipeCasaId == request.EquipeVisitanteId)
        {
            return Result.Failure<Guid>(PartidaErrors.EquipesIguais);
        }

        LigaConfigurada? liga = await ligaRepository.GetAsync(request.LigaId, cancellationToken);

        if (liga is null)
        {
            return Result.Failure<Guid>(LigaConfiguradaErrors.NotFound(request.LigaId));
        }

        Equipe? equipeCasa = await equipeRepository.GetAsync(request.EquipeCasaId, cancellationToken);

        if (equipeCasa is null)
        {
            return Result.Failure<Guid>(EquipeErrors.NotFound(request.EquipeCasaId));
        }

        Equipe? equipeVisitante = await equipeRepository.GetAsync(request.EquipeVisitanteId, cancellationToken);

        if (equipeVisitante is null)
        {
            return Result.Failure<Guid>(EquipeErrors.NotFound(request.EquipeVisitanteId));
        }

        var partida = Partida.Create(
            request.IdExterno,
            request.LigaId,
            request.EquipeCasaId,
            request.EquipeVisitanteId,
            request.DataUtc);

        partidaRepository.Insert(partida);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return partida.Id;
    }
}
