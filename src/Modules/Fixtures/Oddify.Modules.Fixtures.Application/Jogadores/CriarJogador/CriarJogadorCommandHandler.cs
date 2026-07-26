using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Application.Abstractions.Data;
using Oddify.Modules.Fixtures.Domain.Equipes;
using Oddify.Modules.Fixtures.Domain.Jogadores;

namespace Oddify.Modules.Fixtures.Application.Jogadores.CriarJogador;

internal sealed class CriarJogadorCommandHandler(
    IJogadorRepository jogadorRepository,
    IEquipeRepository equipeRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CriarJogadorCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CriarJogadorCommand request, CancellationToken cancellationToken)
    {
        Equipe? equipe = await equipeRepository.GetAsync(request.EquipeId, cancellationToken);

        if (equipe is null)
        {
            return Result.Failure<Guid>(EquipeErrors.NotFound(request.EquipeId));
        }

        var jogador = Jogador.Create(request.IdExterno, request.EquipeId, request.Nome, request.Posicao);

        jogadorRepository.Insert(jogador);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return jogador.Id;
    }
}
