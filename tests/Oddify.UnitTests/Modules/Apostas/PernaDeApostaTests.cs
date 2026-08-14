using FluentAssertions;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;
using Oddify.Modules.Apostas.Domain.PernasDeAposta;

namespace Oddify.UnitTests.Modules.Apostas;

public sealed class PernaDeApostaTests
{
    [Fact]
    public void Resolver_should_set_resultado_ganha_when_ganhou()
    {
        var perna = PernaDeAposta.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "vitoria_casa", 1.5m);

        Result resultado = perna.Resolver(ganhou: true);

        resultado.IsSuccess.Should().BeTrue();
        perna.Resultado.Should().Be(ResultadoDaAposta.Ganha);
    }

    [Fact]
    public void Resolver_should_set_resultado_perdida_when_nao_ganhou()
    {
        var perna = PernaDeAposta.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "vitoria_casa", 1.5m);

        Result resultado = perna.Resolver(ganhou: false);

        resultado.IsSuccess.Should().BeTrue();
        perna.Resultado.Should().Be(ResultadoDaAposta.Perdida);
    }

    [Fact]
    public void Resolver_should_fail_when_already_resolvida()
    {
        var perna = PernaDeAposta.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "vitoria_casa", 1.5m);
        perna.Resolver(true);

        Result resultado = perna.Resolver(false);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(PernaDeApostaErrors.JaResolvida(perna.Id));
    }

    [Fact]
    public void Reabrir_should_set_resultado_pendente_when_resolvida()
    {
        var perna = PernaDeAposta.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "vitoria_casa", 1.5m);
        perna.Resolver(true);

        Result resultado = perna.Reabrir();

        resultado.IsSuccess.Should().BeTrue();
        perna.Resultado.Should().Be(ResultadoDaAposta.Pendente);
    }

    [Fact]
    public void Reabrir_should_fail_when_ainda_pendente()
    {
        var perna = PernaDeAposta.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "vitoria_casa", 1.5m);

        Result resultado = perna.Reabrir();

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(PernaDeApostaErrors.AindaNaoResolvida(perna.Id));
    }

    [Fact]
    public void Anular_should_set_resultado_anulada_when_pendente()
    {
        var perna = PernaDeAposta.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "vitoria_casa", 1.5m);

        Result resultado = perna.Anular();

        resultado.IsSuccess.Should().BeTrue();
        perna.Resultado.Should().Be(ResultadoDaAposta.Anulada);
    }

    [Fact]
    public void Anular_should_fail_when_already_resolvida()
    {
        var perna = PernaDeAposta.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "vitoria_casa", 1.5m);
        perna.Resolver(true);

        Result resultado = perna.Anular();

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(PernaDeApostaErrors.JaResolvida(perna.Id));
    }
}
