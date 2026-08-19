using Oddify.Common.Application.EventBus;

namespace Oddify.Modules.Fixtures.IntegrationEvents;

public sealed class CotacaoColetadaIntegrationEvent(
    Guid id,
    DateTime occurredOnUtc,
    Guid cotacaoId,
    Guid partidaId,
    string mercado,
    decimal odd,
    string casa) : IntegrationEvent(id, occurredOnUtc)
{
    public Guid CotacaoId { get; } = cotacaoId;

    public Guid PartidaId { get; } = partidaId;

    public string Mercado { get; } = mercado;

    public decimal Odd { get; } = odd;

    public string Casa { get; } = casa;
}
