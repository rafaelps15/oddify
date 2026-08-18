using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Application.Abstractions.Data;
using Oddify.Modules.Fixtures.Domain.Equipes;
using Oddify.Modules.Fixtures.Domain.Escalacoes;
using Oddify.Modules.Fixtures.Domain.Partidas;

namespace Oddify.Modules.Fixtures.Application.Escalacoes.RegistrarEscalacao;

internal sealed class RegistrarEscalacaoCommandHandler(
    IEscalacaoRepository escalacaoRepository,
    IPartidaRepository partidaRepository,
    IEquipeRepository equipeRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarEscalacaoCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RegistrarEscalacaoCommand request, CancellationToken cancellationToken)
    {
        Partida? partida = await partidaRepository.GetAsync(request.PartidaId, cancellationToken);

        if (partida is null)
        {
            return Result.Failure<Guid>(PartidaErrors.NotFound(request.PartidaId));
        }

        Equipe? equipe = await equipeRepository.GetAsync(request.EquipeId, cancellationToken);

        if (equipe is null)
        {
            return Result.Failure<Guid>(EquipeErrors.NotFound(request.EquipeId));
        }

        var escalacao = Escalacao.Create(request.PartidaId, request.EquipeId, request.Formacao, request.Tecnico);

        escalacaoRepository.Insert(escalacao);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return escalacao.Id;
    }
}
