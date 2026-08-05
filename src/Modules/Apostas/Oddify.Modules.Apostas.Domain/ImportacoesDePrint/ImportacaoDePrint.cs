using Oddify.Common.Domain;

namespace Oddify.Modules.Apostas.Domain.ImportacoesDePrint;

public sealed class ImportacaoDePrint : Entity
{
    private ImportacaoDePrint()
    {
    }

    public Guid Id { get; private set; }

    public Guid UsuarioId { get; private set; }

    public Guid BancaId { get; private set; }

    public StatusDaImportacao Status { get; private set; }

    public Guid? ApostaMultiplaId { get; private set; }

    public string? MotivoDaFalha { get; private set; }

    public DateTime CriadaEmUtc { get; private set; }

    public DateTime? ProcessadaEmUtc { get; private set; }

    public static ImportacaoDePrint Create(Guid usuarioId, Guid bancaId, DateTime criadaEmUtc)
    {
        var importacao = new ImportacaoDePrint
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            BancaId = bancaId,
            Status = StatusDaImportacao.PendenteDeProcessamento,
            CriadaEmUtc = criadaEmUtc
        };

        importacao.Raise(new ImportacaoDePrintCriadaDomainEvent(importacao.Id));

        return importacao;
    }
}
