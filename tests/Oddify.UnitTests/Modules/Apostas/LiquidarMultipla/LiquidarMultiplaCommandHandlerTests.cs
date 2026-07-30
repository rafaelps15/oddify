using FluentAssertions;
using MediatR;
using NSubstitute;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.Abstractions.Data;
using Oddify.Modules.Apostas.Application.ApostasMultiplas.GetResultadosDasPernas;
using Oddify.Modules.Apostas.Application.ApostasMultiplas.LiquidarMultipla;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;
using Oddify.Modules.Apostas.Domain.Bancas;
using Oddify.Modules.Apostas.Domain.PernasDeAposta;

namespace Oddify.UnitTests.Modules.Apostas.LiquidarMultipla;

public sealed class LiquidarMultiplaCommandHandlerTests
{
    private readonly IApostaMultiplaRepository _apostaMultiplaRepository = Substitute.For<IApostaMultiplaRepository>();
    private readonly IPernaDeApostaRepository _pernaDeApostaRepository = Substitute.For<IPernaDeApostaRepository>();
    private readonly IBancaRepository _bancaRepository = Substitute.For<IBancaRepository>();
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private LiquidarMultiplaCommandHandler CriarHandler() =>
        new(_apostaMultiplaRepository, _pernaDeApostaRepository, _bancaRepository, _sender, _unitOfWork);

    private void ConfigurarResultados(IReadOnlyDictionary<Guid, bool> resultadosPorPernaId)
    {
        _sender.Send(Arg.Any<GetResultadosDasPernasQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyDictionary<Guid, bool>>(resultadosPorPernaId));
    }

    [Fact]
    public async Task Handle_should_mark_multipla_ganha_when_all_pernas_win()
    {
        var banca = Banca.Create(1000m, modoPaperTrading: true);
        var apostaMultipla = ApostaMultipla.Create(banca.Id, oddCombinada: 4.0m, stake: 50m, DateTime.UtcNow);

        var perna1 = PernaDeAposta.Create(apostaMultipla.Id, Guid.NewGuid(), Guid.NewGuid(), "vitoria_casa", 2.0m);
        var perna2 = PernaDeAposta.Create(apostaMultipla.Id, Guid.NewGuid(), Guid.NewGuid(), "vitoria_casa", 2.0m);

        _apostaMultiplaRepository.GetAsync(apostaMultipla.Id, Arg.Any<CancellationToken>()).Returns(apostaMultipla);
        _pernaDeApostaRepository.GetPorApostaMultiplaAsync(apostaMultipla.Id, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<PernaDeAposta>)[perna1, perna2]);

        ConfigurarResultados(new Dictionary<Guid, bool> { [perna1.Id] = true, [perna2.Id] = true });

        _bancaRepository.GetAsync(banca.Id, Arg.Any<CancellationToken>()).Returns(banca);

        Result resultado = await CriarHandler().Handle(new LiquidarMultiplaCommand(apostaMultipla.Id), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        apostaMultipla.Resultado.Should().Be(ResultadoDaAposta.Ganha);
        banca.SaldoAtual.Should().Be(1000m + 50m * (4.0m - 1m));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_mark_multipla_perdida_when_one_perna_loses()
    {
        var banca = Banca.Create(1000m, modoPaperTrading: true);
        var apostaMultipla = ApostaMultipla.Create(banca.Id, oddCombinada: 4.0m, stake: 50m, DateTime.UtcNow);

        var perna1 = PernaDeAposta.Create(apostaMultipla.Id, Guid.NewGuid(), Guid.NewGuid(), "vitoria_casa", 2.0m);
        var perna2 = PernaDeAposta.Create(apostaMultipla.Id, Guid.NewGuid(), Guid.NewGuid(), "vitoria_casa", 2.0m);

        _apostaMultiplaRepository.GetAsync(apostaMultipla.Id, Arg.Any<CancellationToken>()).Returns(apostaMultipla);
        _pernaDeApostaRepository.GetPorApostaMultiplaAsync(apostaMultipla.Id, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<PernaDeAposta>)[perna1, perna2]);

        ConfigurarResultados(new Dictionary<Guid, bool> { [perna1.Id] = true, [perna2.Id] = false });

        _bancaRepository.GetAsync(banca.Id, Arg.Any<CancellationToken>()).Returns(banca);

        Result resultado = await CriarHandler().Handle(new LiquidarMultiplaCommand(apostaMultipla.Id), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        apostaMultipla.Resultado.Should().Be(ResultadoDaAposta.Perdida);
        banca.SaldoAtual.Should().Be(1000m - 50m);
    }

    [Fact]
    public async Task Handle_should_fail_when_query_fails()
    {
        var banca = Banca.Create(1000m, modoPaperTrading: true);
        var apostaMultipla = ApostaMultipla.Create(banca.Id, oddCombinada: 2.0m, stake: 50m, DateTime.UtcNow);

        var perna = PernaDeAposta.Create(apostaMultipla.Id, Guid.NewGuid(), Guid.NewGuid(), "vitoria_casa", 2.0m);

        _apostaMultiplaRepository.GetAsync(apostaMultipla.Id, Arg.Any<CancellationToken>()).Returns(apostaMultipla);
        _pernaDeApostaRepository.GetPorApostaMultiplaAsync(apostaMultipla.Id, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<PernaDeAposta>)[perna]);

        var erro = Error.Problem("ApostasMultiplas.PartidaNaoEncerrada", "A partida ainda não foi encerrada");
        _sender.Send(Arg.Any<GetResultadosDasPernasQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyDictionary<Guid, bool>>(erro));

        Result resultado = await CriarHandler().Handle(new LiquidarMultiplaCommand(apostaMultipla.Id), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(erro);
        apostaMultipla.Resultado.Should().Be(ResultadoDaAposta.Pendente);
    }
}
