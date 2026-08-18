using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Application.Abstractions.Data;
using Oddify.Modules.Fixtures.Domain.Escalacoes;
using Oddify.Modules.Fixtures.Domain.EscalacoesDeJogador;
using Oddify.Modules.Fixtures.Domain.Jogadores;

namespace Oddify.Modules.Fixtures.Application.EscalacoesDeJogador.RegistrarEscalacaoJogador;

internal sealed class RegistrarEscalacaoJogadorCommandHandler(
    IEscalacaoJogadorRepository escalacaoJogadorRepository,
    IEscalacaoRepository escalacaoRepository,
    IJogadorRepository jogadorRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarEscalacaoJogadorCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RegistrarEscalacaoJogadorCommand request, CancellationToken cancellationToken)
    {
        Escalacao? escalacao = await escalacaoRepository.GetAsync(request.EscalacaoId, cancellationToken);

        if (escalacao is null)
        {
            return Result.Failure<Guid>(EscalacaoErrors.NotFound(request.EscalacaoId));
        }

        Jogador? jogador = await jogadorRepository.GetAsync(request.JogadorId, cancellationToken);

        if (jogador is null)
        {
            return Result.Failure<Guid>(JogadorErrors.NotFound(request.JogadorId));
        }

        var escalacaoJogador = EscalacaoJogador.Create(
            request.EscalacaoId,
            request.JogadorId,
            request.Titular,
            request.Posicao,
            request.Numero);

        escalacaoJogadorRepository.Insert(escalacaoJogador);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return escalacaoJogador.Id;
    }
}
