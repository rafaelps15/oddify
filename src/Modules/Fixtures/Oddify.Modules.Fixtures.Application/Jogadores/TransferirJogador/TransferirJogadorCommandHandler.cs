using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Application.Abstractions.Data;
using Oddify.Modules.Fixtures.Domain.Jogadores;

namespace Oddify.Modules.Fixtures.Application.Jogadores.TransferirJogador;

internal sealed class TransferirJogadorCommandHandler(IJogadorRepository jogadorRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<TransferirJogadorCommand>
{
    public async Task<Result> Handle(TransferirJogadorCommand request, CancellationToken cancellationToken)
    {
        Jogador? jogador = await jogadorRepository.GetAsync(request.JogadorId, cancellationToken);

        if (jogador is null)
        {
            return Result.Failure(JogadorErrors.NotFound(request.JogadorId));
        }

        Result result = jogador.TransferirParaEquipe(request.NovaEquipeId);

        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
