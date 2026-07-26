using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Application.Abstractions.Data;
using Oddify.Modules.Fixtures.Domain.Ligas;

namespace Oddify.Modules.Fixtures.Application.Ligas.AtualizarMediasDaLiga;

internal sealed class AtualizarMediasDaLigaCommandHandler(ILigaConfiguradaRepository ligaRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<AtualizarMediasDaLigaCommand>
{
    public async Task<Result> Handle(AtualizarMediasDaLigaCommand request, CancellationToken cancellationToken)
    {
        LigaConfigurada? liga = await ligaRepository.GetAsync(request.LigaId, cancellationToken);

        if (liga is null)
        {
            return Result.Failure(LigaConfiguradaErrors.NotFound(request.LigaId));
        }

        liga.AtualizarMedias(request.MediaDeGols, request.FatorCasa);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
