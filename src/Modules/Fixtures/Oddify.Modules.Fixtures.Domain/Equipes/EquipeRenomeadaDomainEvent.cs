using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Domain.Equipes;

public sealed class EquipeRenomeadaDomainEvent(Guid equipeId, string nome) : DomainEvent
{
    public Guid EquipeId { get; } = equipeId;

    public string Nome { get; } = nome;
}
