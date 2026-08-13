using FluentAssertions;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Domain.AnalisesDisponiveis;

namespace Oddify.UnitTests.Modules.Apostas;

public sealed class AnaliseDisponivelParaApostaTests
{
    private static AnaliseDisponivelParaAposta CriarDisponivel() =>
        AnaliseDisponivelParaAposta.Create(Guid.NewGuid(), Guid.NewGuid(), "vitoria_casa", 2.0m, 0.6m, reduzida: false);

    [Fact]
    public void LiberarUso_should_set_ja_utilizada_false_when_utilizada()
    {
        AnaliseDisponivelParaAposta disponivel = CriarDisponivel();
        disponivel.MarcarComoUtilizada();

        Result resultado = disponivel.LiberarUso();

        resultado.IsSuccess.Should().BeTrue();
        disponivel.JaUtilizada.Should().BeFalse();
    }

    [Fact]
    public void LiberarUso_should_fail_when_ainda_nao_utilizada()
    {
        AnaliseDisponivelParaAposta disponivel = CriarDisponivel();

        Result resultado = disponivel.LiberarUso();

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(AnaliseDisponivelParaApostaErrors.AindaNaoUtilizada(disponivel.Id));
    }
}
