using FluentAssertions;
using Oddify.Modules.Apostas.Domain.ImportacoesDePrint;

namespace Oddify.UnitTests.Modules.Apostas;

public sealed class ImportacaoDePrintTests
{
    [Fact]
    public void Create_should_set_properties_and_raise_ImportacaoDePrintCriadaDomainEvent()
    {
        var usuarioId = Guid.NewGuid();
        var bancaId = Guid.NewGuid();
        DateTime agora = DateTime.UtcNow;

        var importacao = ImportacaoDePrint.Create(usuarioId, bancaId, agora);

        importacao.UsuarioId.Should().Be(usuarioId);
        importacao.BancaId.Should().Be(bancaId);
        importacao.Status.Should().Be(StatusDaImportacao.PendenteDeProcessamento);
        importacao.ApostaMultiplaId.Should().BeNull();
        importacao.MotivoDaFalha.Should().BeNull();
        importacao.CriadaEmUtc.Should().Be(agora);
        importacao.ProcessadaEmUtc.Should().BeNull();
        importacao.DomainEvents.Should().ContainSingle(e => e is ImportacaoDePrintCriadaDomainEvent);
    }
}
