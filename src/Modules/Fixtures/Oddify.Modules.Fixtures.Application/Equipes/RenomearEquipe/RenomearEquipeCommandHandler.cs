using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Application.Abstractions.Data;
using Oddify.Modules.Fixtures.Domain.Equipes;

namespace Oddify.Modules.Fixtures.Application.Equipes.RenomearEquipe;

internal sealed class RenomearEquipeCommandHandler(IEquipeRepository equipeRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<RenomearEquipeCommand>
{
    public async Task<Result> Handle(RenomearEquipeCommand request, CancellationToken cancellationToken)
    {
        Equipe? equipe = await equipeRepository.GetAsync(request.EquipeId, cancellationToken);

        if (equipe is null)
        {
            return Result.Failure(EquipeErrors.NotFound(request.EquipeId));
        }

        equipe.Renomear(request.Nome);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
