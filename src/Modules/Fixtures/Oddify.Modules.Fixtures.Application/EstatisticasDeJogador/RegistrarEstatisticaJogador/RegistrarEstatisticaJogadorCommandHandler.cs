using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Application.Abstractions.Data;
using Oddify.Modules.Fixtures.Domain.EstatisticasDeJogador;
using Oddify.Modules.Fixtures.Domain.Jogadores;
using Oddify.Modules.Fixtures.Domain.Partidas;

namespace Oddify.Modules.Fixtures.Application.EstatisticasDeJogador.RegistrarEstatisticaJogador;

internal sealed class RegistrarEstatisticaJogadorCommandHandler(
    IEstatisticaJogadorRepository estatisticaJogadorRepository,
    IPartidaRepository partidaRepository,
    IJogadorRepository jogadorRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarEstatisticaJogadorCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RegistrarEstatisticaJogadorCommand request, CancellationToken cancellationToken)
    {
        Partida? partida = await partidaRepository.GetAsync(request.PartidaId, cancellationToken);

        if (partida is null)
        {
            return Result.Failure<Guid>(PartidaErrors.NotFound(request.PartidaId));
        }

        Jogador? jogador = await jogadorRepository.GetAsync(request.JogadorId, cancellationToken);

        if (jogador is null)
        {
            return Result.Failure<Guid>(JogadorErrors.NotFound(request.JogadorId));
        }

        var estatistica = EstatisticaJogador.Create(
            request.PartidaId,
            request.JogadorId,
            request.Gols,
            request.Assistencias,
            request.Minutos,
            request.Titular,
            request.Nota);

        estatisticaJogadorRepository.Insert(estatistica);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return estatistica.Id;
    }
}
