using FluentAssertions;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;

namespace Oddify.UnitTests.Modules.Apostas;

public sealed class ApostaMultiplaTests
{
    [Fact]
    public void Create_should_raise_ApostaMultiplaCriadaDomainEvent()
    {
        var apostaMultipla = ApostaMultipla.Create(Guid.NewGuid(), oddCombinada: 4.0m, stake: 50m, DateTime.UtcNow);

        apostaMultipla.DomainEvents.Should().ContainSingle(e => e is ApostaMultiplaCriadaDomainEvent);
        apostaMultipla.Resultado.Should().Be(ResultadoDaAposta.Pendente);
    }

    [Fact]
    public void Liquidar_should_set_lucro_and_resultado_ganha_when_ganhou()
    {
        var apostaMultipla = ApostaMultipla.Create(Guid.NewGuid(), oddCombinada: 4.0m, stake: 50m, DateTime.UtcNow);

        Result resultado = apostaMultipla.Liquidar(ganhou: true);

        resultado.IsSuccess.Should().BeTrue();
        apostaMultipla.Resultado.Should().Be(ResultadoDaAposta.Ganha);
        apostaMultipla.LucroOuPerda.Should().Be(50m * (4.0m - 1));
        apostaMultipla.DomainEvents.Should().Contain(e => e is ApostaMultiplaLiquidadaDomainEvent);
    }

    [Fact]
    public void Liquidar_should_set_lucro_negativo_e_resultado_perdida_when_nao_ganhou()
    {
        var apostaMultipla = ApostaMultipla.Create(Guid.NewGuid(), oddCombinada: 4.0m, stake: 50m, DateTime.UtcNow);

        Result resultado = apostaMultipla.Liquidar(ganhou: false);

        resultado.IsSuccess.Should().BeTrue();
        apostaMultipla.Resultado.Should().Be(ResultadoDaAposta.Perdida);
        apostaMultipla.LucroOuPerda.Should().Be(-50m);
    }

    [Fact]
    public void Liquidar_should_fail_when_already_liquidada()
    {
        var apostaMultipla = ApostaMultipla.Create(Guid.NewGuid(), oddCombinada: 4.0m, stake: 50m, DateTime.UtcNow);
        apostaMultipla.Liquidar(true);

        Result resultado = apostaMultipla.Liquidar(false);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(ApostaMultiplaErrors.JaLiquidada(apostaMultipla.Id));
    }
}
