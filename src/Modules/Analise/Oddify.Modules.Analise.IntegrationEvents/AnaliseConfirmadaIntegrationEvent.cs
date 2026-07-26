using Oddify.Common.Application.EventBus;

namespace Oddify.Modules.Analise.IntegrationEvents;

public sealed class AnaliseConfirmadaIntegrationEvent(
    Guid id,
    DateTime occurredOnUtc,
    Guid analiseId,
    Guid partidaId,
    string mercado,
    decimal oddDeMercado,
    decimal probabilidadeConfirmada,
    bool reduzida)
    : IntegrationEvent(id, occurredOnUtc)
{
    public Guid AnaliseId { get; } = analiseId;

    public Guid PartidaId { get; } = partidaId;

    public string Mercado { get; } = mercado;

    public decimal OddDeMercado { get; } = oddDeMercado;

    public decimal ProbabilidadeConfirmada { get; } = probabilidadeConfirmada;

    public bool Reduzida { get; } = reduzida;
}
