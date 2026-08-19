using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Analise.Application.Abstractions.Data;
using Oddify.Modules.Analise.Domain.Fixtures;

namespace Oddify.Modules.Analise.Application.Fixtures.UpsertLiga;

internal sealed class UpsertLigaCommandHandler(ILigaRepository ligaRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<UpsertLigaCommand>
{
    public async Task<Result> Handle(UpsertLigaCommand request, CancellationToken cancellationToken)
    {
        Liga? liga = await ligaRepository.GetAsync(request.LigaId, cancellationToken);

        if (liga is null)
        {
            ligaRepository.Insert(Liga.Create(request.LigaId, request.Nome, request.MediaDeGols, request.FatorCasa, request.Calibrada));
        }
        else
        {
            liga.Atualizar(request.Nome, request.MediaDeGols, request.FatorCasa, request.Calibrada);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
