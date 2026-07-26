using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Application.Abstractions.Data;
using Oddify.Modules.Fixtures.Domain.Ligas;

namespace Oddify.Modules.Fixtures.Application.Ligas.CalibrarLiga;

internal sealed class CalibrarLigaCommandHandler(ILigaConfiguradaRepository ligaRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<CalibrarLigaCommand>
{
    public async Task<Result> Handle(CalibrarLigaCommand request, CancellationToken cancellationToken)
    {
        LigaConfigurada? liga = await ligaRepository.GetAsync(request.LigaId, cancellationToken);

        if (liga is null)
        {
            return Result.Failure(LigaConfiguradaErrors.NotFound(request.LigaId));
        }

        liga.Calibrar();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
