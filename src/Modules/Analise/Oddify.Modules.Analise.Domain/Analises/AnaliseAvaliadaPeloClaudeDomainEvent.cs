using Oddify.Common.Domain;

namespace Oddify.Modules.Analise.Domain.Analises;

public sealed class AnaliseAvaliadaPeloClaudeDomainEvent(Guid analiseId, DecisaoDoClaude decisao) : DomainEvent
{
    public Guid AnaliseId { get; } = analiseId;

    public DecisaoDoClaude Decisao { get; } = decisao;
}
