using Oddify.Common.Domain;

namespace Oddify.Modules.Analise.Domain.Analises;

public sealed class AnaliseCriadaDomainEvent(Guid analiseId) : DomainEvent
{
    public Guid AnaliseId { get; } = analiseId;
}
