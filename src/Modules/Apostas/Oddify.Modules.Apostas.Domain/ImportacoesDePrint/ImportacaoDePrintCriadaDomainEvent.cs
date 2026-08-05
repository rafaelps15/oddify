using Oddify.Common.Domain;

namespace Oddify.Modules.Apostas.Domain.ImportacoesDePrint;

public sealed class ImportacaoDePrintCriadaDomainEvent(Guid importacaoDePrintId) : DomainEvent
{
    public Guid ImportacaoDePrintId { get; } = importacaoDePrintId;
}
