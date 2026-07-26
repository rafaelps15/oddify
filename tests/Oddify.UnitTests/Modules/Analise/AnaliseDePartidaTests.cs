using FluentAssertions;
using Oddify.Common.Domain;
using Oddify.Modules.Analise.Domain.Analises;

namespace Oddify.UnitTests.Modules.Analise;

public sealed class AnaliseDePartidaTests
{
    [Fact]
    public void Create_should_raise_AnaliseCriadaDomainEvent()
    {
        AnaliseDePartida analise = CriarAnaliseAprovada();

        analise.DomainEvents.Should().ContainSingle(e => e is AnaliseCriadaDomainEvent);
    }

    [Fact]
    public void Create_should_set_decisao_as_NaoAvaliada()
    {
        AnaliseDePartida analise = CriarAnaliseAprovada();

        analise.DecisaoDoClaude.Should().Be(DecisaoDoClaude.NaoAvaliada);
    }

    [Fact]
    public void RegistrarDecisaoDoClaude_should_fail_when_not_aprovada_no_filtro()
    {
        var analise = AnaliseDePartida.Create(
            Guid.NewGuid(), "vitoria_casa", 0.5m, 0.5m, 0.5m, 0.0m, 1.5m,
            aprovadaNoFiltro: false, motivoDoDescarte: "Vantagem insuficiente", DateTime.UtcNow);

        Result resultado = analise.RegistrarDecisaoDoClaude(DecisaoDoClaude.Confirma, "ok", "{}", "v1");

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(AnaliseDePartidaErrors.NaoAprovadaNoFiltro(analise.Id));
    }

    [Fact]
    public void RegistrarDecisaoDoClaude_should_succeed_and_raise_event_when_aprovada()
    {
        AnaliseDePartida analise = CriarAnaliseAprovada();

        Result resultado = analise.RegistrarDecisaoDoClaude(DecisaoDoClaude.Confirma, "justificativa", "{\"decisao\":\"CONFIRMA\"}", "avaliador-critico-v1");

        resultado.IsSuccess.Should().BeTrue();
        analise.DecisaoDoClaude.Should().Be(DecisaoDoClaude.Confirma);
        analise.JustificativaDoClaude.Should().Be("justificativa");
        analise.DomainEvents.Should().Contain(e => e is AnaliseAvaliadaPeloClaudeDomainEvent);
    }

    private static AnaliseDePartida CriarAnaliseAprovada() =>
        AnaliseDePartida.Create(
            Guid.NewGuid(), "vitoria_casa", 0.55m, 0.55m, 0.5m, 0.05m, 1.5m,
            aprovadaNoFiltro: true, motivoDoDescarte: null, DateTime.UtcNow);
}
