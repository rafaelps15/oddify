using FluentAssertions;
using FluentValidation.Results;
using Oddify.Modules.Analise.Application.Analises.AnalisarPartida;

namespace Oddify.UnitTests.Modules.Analise.AnalisarPartida;

public sealed class AnalisarPartidaCommandValidatorTests
{
    private readonly AnalisarPartidaCommandValidator _validator = new();

    [Fact]
    public void Validate_should_succeed_for_known_mercado()
    {
        ValidationResult resultado = _validator.Validate(new AnalisarPartidaCommand(Guid.NewGuid(), "vitoria_casa"));

        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_should_fail_for_unknown_mercado()
    {
        ValidationResult resultado = _validator.Validate(new AnalisarPartidaCommand(Guid.NewGuid(), "mercado_inexistente"));

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "Mercado");
    }
}
