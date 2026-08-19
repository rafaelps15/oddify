using Oddify.Common.Application.EventBus;

namespace Oddify.Modules.Fixtures.IntegrationEvents;

// Um único evento pras 3 mudanças de LigaConfigurada (criação, médias atualizadas, calibração
// alterada) — cada handler de domain event reconsulta o estado completo e publica este mesmo
// snapshot (mesmo padrão do §10: nunca confiar só no payload do domain event). Quem consome só
// precisa fazer um upsert do estado atual, não reagir a qual campo mudou especificamente.
public sealed class LigaAtualizadaIntegrationEvent(
    Guid id,
    DateTime occurredOnUtc,
    Guid ligaId,
    string nome,
    decimal mediaDeGols,
    decimal fatorCasa,
    bool calibrada) : IntegrationEvent(id, occurredOnUtc)
{
    public Guid LigaId { get; } = ligaId;

    public string Nome { get; } = nome;

    public decimal MediaDeGols { get; } = mediaDeGols;

    public decimal FatorCasa { get; } = fatorCasa;

    public bool Calibrada { get; } = calibrada;
}
