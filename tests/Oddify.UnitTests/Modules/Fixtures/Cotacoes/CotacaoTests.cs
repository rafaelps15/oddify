using FluentAssertions;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Domain.Cotacoes;

namespace Oddify.UnitTests.Modules.Fixtures.Cotacoes;

public sealed class CotacaoTests
{
    [Fact]
    public void Create_should_succeed_and_raise_event_when_odd_is_valid()
    {
        Result<Cotacao> resultado = Cotacao.Create(Guid.NewGuid(), "vitoria_casa", 1.5m, "casa-de-apostas", DateTime.UtcNow);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.DomainEvents.Should().ContainSingle(e => e is CotacaoColetadaDomainEvent);
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(0.5)]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_should_fail_when_odd_is_not_greater_than_one(decimal odd)
    {
        Result<Cotacao> resultado = Cotacao.Create(Guid.NewGuid(), "vitoria_casa", odd, "casa-de-apostas", DateTime.UtcNow);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(CotacaoErrors.OddInvalida);
    }
}
