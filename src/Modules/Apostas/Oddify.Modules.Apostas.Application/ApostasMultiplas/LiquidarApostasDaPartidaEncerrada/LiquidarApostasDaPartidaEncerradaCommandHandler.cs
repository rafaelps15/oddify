using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.Abstractions.Data;
using Oddify.Modules.Apostas.Application.ApostasMultiplas.LiquidarMultipla;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.LiquidarApostasDaPartidaEncerrada;

// Disparado pelo PartidaEncerradaIntegrationEventConsumer (Presentation/IntegrationEvents) — enfileira
// o mesmo LiquidarMultiplaCommand que o endpoint usa, um por aposta pendente, via ICommandsScheduler
// em vez de reenviar síncrono via ISender.Send: cada item é resolvido no seu próprio ritmo pelo
// InternalCommandProcessorJob, em vez de todas as N liquidações rodarem sequenciais dentro desta
// mesma execução (mesmo padrão do modular-monolith-with-ddd para reagir a um evento em lote —
// reenviar o Command, não duplicar ou compartilhar a lógica de liquidação numa classe à parte).
// Uma múltipla com outra perna ainda pendente em partida diferente falha dentro do
// LiquidarMultiplaCommandHandler (partida não encerrada) quando o job processar aquela linha — não
// há Result pra inspecionar aqui, o enqueue só grava a linha, então não tem "melhor esforço" pra
// decidir neste handler: toda pendente é sempre enfileirada, sucesso/falha é resolvido depois.
internal sealed class LiquidarApostasDaPartidaEncerradaCommandHandler(
    IApostaMultiplaRepository apostaMultiplaRepository,
    ICommandsScheduler commandsScheduler,
    IUnitOfWork unitOfWork)
    : ICommandHandler<LiquidarApostasDaPartidaEncerradaCommand>
{
    public async Task<Result> Handle(LiquidarApostasDaPartidaEncerradaCommand request, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<ApostaMultipla> apostasPendentes =
            await apostaMultiplaRepository.GetPendentesPorPartidaAsync(request.PartidaId, cancellationToken);

        foreach (ApostaMultipla apostaMultipla in apostasPendentes)
        {
            await commandsScheduler.EnqueueAsync(new LiquidarMultiplaCommand(apostaMultipla.Id, apostaMultipla.UsuarioId));
        }

        // ICommandsScheduler.EnqueueAsync só marca a linha no ChangeTracker (mesmo mecanismo do
        // IOutboxWriter) — precisa deste SaveChanges pra persistir de verdade.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
