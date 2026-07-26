using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Application.Abstractions.Data;
using Oddify.Modules.Fixtures.Domain.Ligas;

namespace Oddify.Modules.Fixtures.Application.Ligas.CriarLiga;

internal sealed class CriarLigaCommandHandler(ILigaConfiguradaRepository ligaRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<CriarLigaCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CriarLigaCommand request, CancellationToken cancellationToken)
    {
        LigaConfigurada? existente = await ligaRepository.GetByIdExternoAsync(request.IdExterno, cancellationToken);

        if (existente is not null)
        {
            return Result.Failure<Guid>(LigaConfiguradaErrors.IdExternoJaCadastrado);
        }

        var liga = LigaConfigurada.Create(request.IdExterno, request.Nome, request.MediaDeGols, request.FatorCasa);

        ligaRepository.Insert(liga);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return liga.Id;
    }
}
