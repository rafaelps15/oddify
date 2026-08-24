using FluentAssertions;
using FluentValidation.Results;
using Oddify.Modules.Analise.Application.Fixtures.RegistrarCotacao;

namespace Oddify.UnitTests.Modules.Analise.Fixtures;

public sealed class RegistrarCotacaoCommandValidatorTests
{
    private readonly RegistrarCotacaoCommandValidator _validator = new();

    private static RegistrarCotacaoCommand CriarComando(string mercado) => new(
        Guid.NewGuid(), Guid.NewGuid(), mercado, 1.85m, "bet365", DateTime.UtcNow);

    [Fact]
    public void Validate_should_succeed_for_known_mercado()
    {
        ValidationResult resultado = _validator.Validate(CriarComando("vitoria_casa"));

        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_should_fail_for_unknown_mercado()
    {
        ValidationResult resultado = _validator.Validate(CriarComando("mercado_inexistente"));

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "Mercado");
    }
}
