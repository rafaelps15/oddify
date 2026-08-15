using FluentAssertions;
using NSubstitute;
using Oddify.Common.Domain;
using Oddify.Modules.Analise.PublicApi;
using Oddify.Modules.Apostas.Application.ApostasMultiplas.GetResultadosDasPernas;
using Oddify.Modules.Fixtures.PublicApi;

namespace Oddify.UnitTests.Modules.Apostas.GetResultadosDasPernas;

public sealed class GetResultadosDasPernasQueryHandlerTests
{
    private readonly IFixturesApi _fixturesApi = Substitute.For<IFixturesApi>();
    private readonly IAnaliseApi _analiseApi = Substitute.For<IAnaliseApi>();

    private GetResultadosDasPernasQueryHandler CriarHandler() => new(_fixturesApi, _analiseApi);

    private static PartidaResponse PartidaEncerrada(Guid id, int golsCasa, int golsVisitante) =>
        new(id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, "Encerrada", golsCasa, golsVisitante);

    private void ConfigurarPartidas(params PartidaResponse[] partidas) =>
        _fixturesApi.ObterPartidasAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<PartidaResponse>)partidas);

    [Fact]
    public async Task Handle_should_resolve_each_perna_by_id()
    {
        var pernaId1 = Guid.NewGuid();
        var pernaId2 = Guid.NewGuid();
        var partida1 = Guid.NewGuid();
        var partida2 = Guid.NewGuid();

        ConfigurarPartidas(PartidaEncerrada(partida1, 2, 0), PartidaEncerrada(partida2, 0, 1));

        _analiseApi.ResolverMercado("vitoria_casa", 2, 0).Returns(true);
        _analiseApi.ResolverMercado("vitoria_casa", 0, 1).Returns(false);

        var query = new GetResultadosDasPernasQuery(
            [new PernaParaResolver(pernaId1, partida1, "vitoria_casa"), new PernaParaResolver(pernaId2, partida2, "vitoria_casa")]);

        Result<IReadOnlyDictionary<Guid, bool>> resultado = await CriarHandler().Handle(query, CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value[pernaId1].Should().BeTrue();
        resultado.Value[pernaId2].Should().BeFalse();
    }

    [Fact]
    public async Task Handle_should_fail_when_a_partida_is_not_yet_encerrada()
    {
        var pernaId = Guid.NewGuid();
        var partidaId = Guid.NewGuid();

        ConfigurarPartidas(new PartidaResponse(partidaId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, "Agendada", null, null));

        var query = new GetResultadosDasPernasQuery([new PernaParaResolver(pernaId, partidaId, "vitoria_casa")]);

        Result<IReadOnlyDictionary<Guid, bool>> resultado = await CriarHandler().Handle(query, CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Code.Should().Be("ApostasMultiplas.PartidaNaoEncerrada");
    }

    [Fact]
    public async Task Handle_should_fail_when_partida_fetch_fails()
    {
        var pernaId = Guid.NewGuid();
        var partidaId = Guid.NewGuid();

        ConfigurarPartidas();

        var query = new GetResultadosDasPernasQuery([new PernaParaResolver(pernaId, partidaId, "vitoria_casa")]);

        Result<IReadOnlyDictionary<Guid, bool>> resultado = await CriarHandler().Handle(query, CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Code.Should().Be("ApostasMultiplas.PartidaNaoEncontrada");
    }
}
