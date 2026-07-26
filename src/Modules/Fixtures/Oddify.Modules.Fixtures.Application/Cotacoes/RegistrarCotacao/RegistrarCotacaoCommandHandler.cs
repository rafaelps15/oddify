using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Application.Abstractions.Data;
using Oddify.Modules.Fixtures.Domain.Cotacoes;
using Oddify.Modules.Fixtures.Domain.Partidas;

namespace Oddify.Modules.Fixtures.Application.Cotacoes.RegistrarCotacao;

internal sealed class RegistrarCotacaoCommandHandler(
    ICotacaoRepository cotacaoRepository,
    IPartidaRepository partidaRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarCotacaoCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RegistrarCotacaoCommand request, CancellationToken cancellationToken)
    {
        Partida? partida = await partidaRepository.GetAsync(request.PartidaId, cancellationToken);

        if (partida is null)
        {
            return Result.Failure<Guid>(PartidaErrors.NotFound(request.PartidaId));
        }

        Result<Cotacao> result = Cotacao.Create(request.PartidaId, request.Mercado, request.Odd, request.Casa, request.ColetadaEmUtc);

        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        Cotacao cotacao = result.Value;

        cotacaoRepository.Insert(cotacao);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return cotacao.Id;
    }
}
