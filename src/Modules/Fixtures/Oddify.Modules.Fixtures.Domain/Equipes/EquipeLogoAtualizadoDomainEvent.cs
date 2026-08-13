using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Domain.Equipes;

public sealed class EquipeLogoAtualizadoDomainEvent(Guid equipeId, string? logo) : DomainEvent
{
    public Guid EquipeId { get; } = equipeId;

    public string? Logo { get; } = logo;
}
