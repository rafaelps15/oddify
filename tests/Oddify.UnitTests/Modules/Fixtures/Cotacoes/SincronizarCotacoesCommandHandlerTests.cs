using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Application.Abstractions.Data;
using Oddify.Modules.Fixtures.Application.Abstractions.ExternalData;
using Oddify.Modules.Fixtures.Application.Cotacoes.SincronizarCotacoes;
using Oddify.Modules.Fixtures.Domain.Cotacoes;
using Oddify.Modules.Fixtures.Domain.Equipes;
using Oddify.Modules.Fixtures.Domain.Ligas;
using Oddify.Modules.Fixtures.Domain.Partidas;

namespace Oddify.UnitTests.Modules.Fixtures.Cotacoes;

public sealed class SincronizarCotacoesCommandHandlerTests
{
    private readonly IPartidaRepository _partidaRepository = Substitute.For<IPartidaRepository>();
    private readonly IEquipeRepository _equipeRepository = Substitute.For<IEquipeRepository>();
    private readonly ILigaConfiguradaRepository _ligaRepository = Substitute.For<ILigaConfiguradaRepository>();
    private readonly ICotacaoRepository _cotacaoRepository = Substitute.For<ICotacaoRepository>();
    private readonly ITheOddsApiClient _theOddsApiClient = Substitute.For<ITheOddsApiClient>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private static readonly LigaConfigurada Liga = LigaConfigurada.Create("liga-1", "Premier League", 2.5m, 1.1m, bandeira: null);
    private static readonly Equipe EquipeCasa = Equipe.Create("time-casa", "Flamengo", Liga.Id, logo: null);
    private static readonly Equipe EquipeVisitante = Equipe.Create("time-visitante", "Palmeiras", Liga.Id, logo: null);
    private static readonly DateTime DataDaPartida = DateTime.UtcNow.AddHours(6);

    private SincronizarCotacoesCommandHandler CriarHandler(SincronizacaoExternaOptions opcoes) =>
        new(_partidaRepository, _equipeRepository, _ligaRepository, _cotacaoRepository, _theOddsApiClient, Options.Create(opcoes), _unitOfWork);

    private Partida CriarPartidaProxima()
    {
        var partida = Partida.Create("fixture-1", Liga.Id, EquipeCasa.Id, EquipeVisitante.Id, DataDaPartida, rodada: 1, temporada: 2026);

        _partidaRepository.ListarAgendadasEntreAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<Partida>)[partida]);
        _ligaRepository.GetAsync(Liga.Id, Arg.Any<CancellationToken>()).Returns(Liga);
        _equipeRepository.GetAsync(EquipeCasa.Id, Arg.Any<CancellationToken>()).Returns(EquipeCasa);
        _equipeRepository.GetAsync(EquipeVisitante.Id, Arg.Any<CancellationToken>()).Returns(EquipeVisitante);

        return partida;
    }

    [Fact]
    public async Task Handle_should_register_cotacoes_when_evento_matches_confidently()
    {
        Partida partida = CriarPartidaProxima();

        var opcoes = new SincronizacaoExternaOptions { TheOddsApiSportKeys = { ["liga-1"] = "soccer_epl" } };

        var evento = new EventoDeOddsExternoDto(
            "evento-1",
            "Flamengo",
            "Palmeiras",
            DataDaPartida.AddMinutes(30),
            [new OutcomeDeOddsDto("Bet365", "vitoria_casa", 2.1m), new OutcomeDeOddsDto("Bet365", "empate", 3.2m)]);

        _theOddsApiClient.GetOddsAsync("soccer_epl", Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<EventoDeOddsExternoDto>>([evento]));

        Result resultado = await CriarHandler(opcoes).Handle(new SincronizarCotacoesCommand(), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        _cotacaoRepository.Received(1).Insert(Arg.Is<Cotacao>(c => c.PartidaId == partida.Id && c.Mercado == "vitoria_casa" && c.Odd == 2.1m));
        _cotacaoRepository.Received(1).Insert(Arg.Is<Cotacao>(c => c.PartidaId == partida.Id && c.Mercado == "empate" && c.Odd == 3.2m));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_skip_partida_when_no_confident_match()
    {
        CriarPartidaProxima();

        var opcoes = new SincronizacaoExternaOptions { TheOddsApiSportKeys = { ["liga-1"] = "soccer_epl" } };

        var eventoDeOutraPartida = new EventoDeOddsExternoDto(
            "evento-2",
            "Corinthians",
            "Santos",
            DataDaPartida,
            [new OutcomeDeOddsDto("Bet365", "vitoria_casa", 1.9m)]);

        _theOddsApiClient.GetOddsAsync("soccer_epl", Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<EventoDeOddsExternoDto>>([eventoDeOutraPartida]));

        Result resultado = await CriarHandler(opcoes).Handle(new SincronizarCotacoesCommand(), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        _cotacaoRepository.DidNotReceive().Insert(Arg.Any<Cotacao>());
    }

    [Fact]
    public async Task Handle_should_skip_liga_when_sportKey_not_mapped()
    {
        CriarPartidaProxima();

        var opcoes = new SincronizacaoExternaOptions();

        Result resultado = await CriarHandler(opcoes).Handle(new SincronizarCotacoesCommand(), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        await _theOddsApiClient.DidNotReceive().GetOddsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        _cotacaoRepository.DidNotReceive().Insert(Arg.Any<Cotacao>());
    }
}
