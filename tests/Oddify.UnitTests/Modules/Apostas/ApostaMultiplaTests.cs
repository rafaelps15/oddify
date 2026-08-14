using FluentAssertions;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;

namespace Oddify.UnitTests.Modules.Apostas;

public sealed class ApostaMultiplaTests
{
    private static ApostaMultipla CriarApostaMultipla(decimal oddCombinada, decimal stake) =>
        ApostaMultipla.Create(Guid.NewGuid(), Guid.NewGuid(), oddCombinada, stake, OrigemDaAposta.ManualEntry, descricao: null, passoDaJornadaId: null, DateTime.UtcNow);

    [Fact]
    public void Create_should_raise_ApostaMultiplaCriadaDomainEvent()
    {
        ApostaMultipla apostaMultipla = CriarApostaMultipla(oddCombinada: 4.0m, stake: 50m);

        apostaMultipla.DomainEvents.Should().ContainSingle(e => e is ApostaMultiplaCriadaDomainEvent);
        apostaMultipla.Resultado.Should().Be(ResultadoDaAposta.Pendente);
    }

    [Fact]
    public void Liquidar_should_set_lucro_and_resultado_ganha_when_ganhou()
    {
        ApostaMultipla apostaMultipla = CriarApostaMultipla(oddCombinada: 4.0m, stake: 50m);

        Result resultado = apostaMultipla.Liquidar(ganhou: true, DateTime.UtcNow);

        resultado.IsSuccess.Should().BeTrue();
        apostaMultipla.Resultado.Should().Be(ResultadoDaAposta.Ganha);
        apostaMultipla.LucroOuPerda.Should().Be(50m * (4.0m - 1));
        apostaMultipla.DomainEvents.Should().Contain(e => e is ApostaMultiplaLiquidadaDomainEvent);
    }

    [Fact]
    public void Liquidar_should_set_lucro_negativo_e_resultado_perdida_when_nao_ganhou()
    {
        ApostaMultipla apostaMultipla = CriarApostaMultipla(oddCombinada: 4.0m, stake: 50m);

        Result resultado = apostaMultipla.Liquidar(ganhou: false, DateTime.UtcNow);

        resultado.IsSuccess.Should().BeTrue();
        apostaMultipla.Resultado.Should().Be(ResultadoDaAposta.Perdida);
        apostaMultipla.LucroOuPerda.Should().Be(-50m);
    }

    [Fact]
    public void Liquidar_should_fail_when_already_liquidada()
    {
        ApostaMultipla apostaMultipla = CriarApostaMultipla(oddCombinada: 4.0m, stake: 50m);
        apostaMultipla.Liquidar(true, DateTime.UtcNow);

        Result resultado = apostaMultipla.Liquidar(false, DateTime.UtcNow);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(ApostaMultiplaErrors.JaLiquidada(apostaMultipla.Id));
    }

    [Fact]
    public void Estornar_should_return_previous_lucro_and_set_resultado_pendente()
    {
        ApostaMultipla apostaMultipla = CriarApostaMultipla(oddCombinada: 4.0m, stake: 50m);
        apostaMultipla.Liquidar(ganhou: true, DateTime.UtcNow);

        Result<decimal> resultado = apostaMultipla.Estornar(DateTime.UtcNow);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().Be(50m * (4.0m - 1));
        apostaMultipla.Resultado.Should().Be(ResultadoDaAposta.Pendente);
        apostaMultipla.LucroOuPerda.Should().BeNull();
        apostaMultipla.DomainEvents.Should().Contain(e => e is ApostaMultiplaEstornadaDomainEvent);
    }

    [Fact]
    public void Estornar_should_fail_when_ainda_pendente()
    {
        ApostaMultipla apostaMultipla = CriarApostaMultipla(oddCombinada: 4.0m, stake: 50m);

        Result<decimal> resultado = apostaMultipla.Estornar(DateTime.UtcNow);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(ApostaMultiplaErrors.AindaNaoLiquidada(apostaMultipla.Id));
    }

    [Fact]
    public void Anular_should_set_resultado_anulada_and_lucro_zero_when_pendente()
    {
        ApostaMultipla apostaMultipla = CriarApostaMultipla(oddCombinada: 4.0m, stake: 50m);

        Result resultado = apostaMultipla.Anular(DateTime.UtcNow);

        resultado.IsSuccess.Should().BeTrue();
        apostaMultipla.Resultado.Should().Be(ResultadoDaAposta.Anulada);
        apostaMultipla.LucroOuPerda.Should().Be(0m);
        apostaMultipla.DomainEvents.Should().Contain(e => e is ApostaMultiplaAnuladaDomainEvent);
    }

    [Fact]
    public void Anular_should_fail_when_already_liquidada()
    {
        ApostaMultipla apostaMultipla = CriarApostaMultipla(oddCombinada: 4.0m, stake: 50m);
        apostaMultipla.Liquidar(true, DateTime.UtcNow);

        Result resultado = apostaMultipla.Anular(DateTime.UtcNow);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(ApostaMultiplaErrors.JaDecidida(apostaMultipla.Id));
    }
}
