using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Domain.Ligas;

public sealed class LigaMediasAtualizadasDomainEvent(Guid ligaId, decimal mediaDeGols, decimal fatorCasa) : DomainEvent
{
    public Guid LigaId { get; } = ligaId;

    public decimal MediaDeGols { get; } = mediaDeGols;

    public decimal FatorCasa { get; } = fatorCasa;
}
