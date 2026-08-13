using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Application.Abstractions.Data;
using Oddify.Modules.Fixtures.Domain.Equipes;
using Oddify.Modules.Fixtures.Domain.Ligas;

namespace Oddify.Modules.Fixtures.Application.Equipes.CriarEquipe;

internal sealed class CriarEquipeCommandHandler(
    IEquipeRepository equipeRepository,
    ILigaConfiguradaRepository ligaRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CriarEquipeCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CriarEquipeCommand request, CancellationToken cancellationToken)
    {
        LigaConfigurada? liga = await ligaRepository.GetAsync(request.LigaId, cancellationToken);

        if (liga is null)
        {
            return Result.Failure<Guid>(LigaConfiguradaErrors.NotFound(request.LigaId));
        }

        var equipe = Equipe.Create(request.IdExterno, request.Nome, request.LigaId, logo: null);

        equipeRepository.Insert(equipe);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return equipe.Id;
    }
}
