using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Domain.Equipes;

public sealed class EquipeCriadaDomainEvent(Guid equipeId) : DomainEvent
{
    public Guid EquipeId { get; } = equipeId;
}
