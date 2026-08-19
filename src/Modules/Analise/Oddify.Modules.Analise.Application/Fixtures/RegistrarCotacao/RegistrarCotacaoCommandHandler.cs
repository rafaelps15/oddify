using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Analise.Application.Abstractions.Data;
using Oddify.Modules.Analise.Domain.Fixtures;

namespace Oddify.Modules.Analise.Application.Fixtures.RegistrarCotacao;

// Idempotente: CotacaoColetadaIntegrationEvent é publicado via IEventBus.PublishAsync direto
// (não-durável, ver comentário em CotacaoColetadaDomainEventHandler), mas o inbox do consumidor
// ainda é at-least-once — se a cotação espelhada já existe, não duplica.
internal sealed class RegistrarCotacaoCommandHandler(ICotacaoRepository cotacaoRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarCotacaoCommand>
{
    public async Task<Result> Handle(RegistrarCotacaoCommand request, CancellationToken cancellationToken)
    {
        Cotacao? cotacaoExistente = await cotacaoRepository.GetMaisRecenteAsync(request.PartidaId, request.Mercado, cancellationToken);
        if (cotacaoExistente is not null && cotacaoExistente.Id == request.CotacaoId)
        {
            return Result.Success();
        }

        cotacaoRepository.Insert(
            Cotacao.Create(request.CotacaoId, request.PartidaId, request.Mercado, request.Odd, request.Casa, request.ColetadaEmUtc));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
