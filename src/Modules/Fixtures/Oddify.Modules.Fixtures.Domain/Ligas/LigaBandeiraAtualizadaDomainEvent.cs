using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Domain.Ligas;

public sealed class LigaBandeiraAtualizadaDomainEvent(Guid ligaId, string? bandeira) : DomainEvent
{
    public Guid LigaId { get; } = ligaId;

    public string? Bandeira { get; } = bandeira;
}
