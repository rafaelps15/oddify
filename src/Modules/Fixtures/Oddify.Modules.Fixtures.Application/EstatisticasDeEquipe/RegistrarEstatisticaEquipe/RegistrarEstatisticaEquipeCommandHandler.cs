using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Application.Abstractions.Data;
using Oddify.Modules.Fixtures.Domain.Equipes;
using Oddify.Modules.Fixtures.Domain.EstatisticasDeEquipe;
using Oddify.Modules.Fixtures.Domain.Partidas;

namespace Oddify.Modules.Fixtures.Application.EstatisticasDeEquipe.RegistrarEstatisticaEquipe;

internal sealed class RegistrarEstatisticaEquipeCommandHandler(
    IEstatisticaEquipeRepository estatisticaEquipeRepository,
    IPartidaRepository partidaRepository,
    IEquipeRepository equipeRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarEstatisticaEquipeCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RegistrarEstatisticaEquipeCommand request, CancellationToken cancellationToken)
    {
        Partida? partida = await partidaRepository.GetAsync(request.PartidaId, cancellationToken);

        if (partida is null)
        {
            return Result.Failure<Guid>(PartidaErrors.NotFound(request.PartidaId));
        }

        Equipe? equipe = await equipeRepository.GetAsync(request.EquipeId, cancellationToken);

        if (equipe is null)
        {
            return Result.Failure<Guid>(EquipeErrors.NotFound(request.EquipeId));
        }

        var estatistica = EstatisticaEquipe.Create(
            request.PartidaId,
            request.EquipeId,
            request.Gols,
            request.Finalizacoes,
            request.Escanteios,
            request.Posse);

        estatisticaEquipeRepository.Insert(estatistica);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return estatistica.Id;
    }
}
