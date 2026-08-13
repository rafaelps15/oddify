using FluentAssertions;
using NSubstitute;
using Oddify.Common.Application.Clock;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.Abstractions.Data;
using Oddify.Modules.Apostas.Application.JornadasDeAlavancagem.AvaliarPassoDaJornada;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;
using Oddify.Modules.Apostas.Domain.Bancas;
using Oddify.Modules.Apostas.Domain.JornadasDeAlavancagem;
using Oddify.Modules.Apostas.Domain.PassosDaJornada;

namespace Oddify.UnitTests.Modules.Apostas.AvaliarPassoDaJornada;

public sealed class AvaliarPassoDaJornadaCommandHandlerTests
{
    private readonly IPassoDaJornadaRepository _passoDaJornadaRepository = Substitute.For<IPassoDaJornadaRepository>();
    private readonly IJornadaDeAlavancagemRepository _jornadaDeAlavancagemRepository = Substitute.For<IJornadaDeAlavancagemRepository>();
    private readonly IBancaRepository _bancaRepository = Substitute.For<IBancaRepository>();
    private readonly IApostaMultiplaRepository _apostaMultiplaRepository = Substitute.For<IApostaMultiplaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly Guid _usuarioId = Guid.NewGuid();

    private AvaliarPassoDaJornadaCommandHandler CriarHandler()
    {
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);
        return new(_passoDaJornadaRepository, _jornadaDeAlavancagemRepository, _bancaRepository, _apostaMultiplaRepository, _unitOfWork, _dateTimeProvider);
    }

    private Banca CriarBanca(decimal saldoAtual) =>
        Banca.Create(_usuarioId, "Banca de alavancagem", saldoAtual, 1m, PerfilDeRisco.Agressivo, modoPaperTrading: false, FinalidadeDaBanca.Alavancagem, DateTime.UtcNow);

    private static PassoDaJornada CriarPasso(Guid jornadaId, int numeroDeApostas, StatusDoPasso status = StatusDoPasso.EmAberto)
    {
        var passo = PassoDaJornada.Create(jornadaId, numero: 1, valorDoPasso: 75m, numeroDeApostas, DateTime.UtcNow);

        if (status == StatusDoPasso.EmAberto)
        {
            passo.MarcarEmAberto();
        }
        else if (status == StatusDoPasso.Avancou)
        {
            passo.MarcarEmAberto();
            passo.MarcarAvancou(valorResultante: 150m);
        }

        return passo;
    }

    private ApostaMultipla CriarApostaLiquidada(Guid bancaId, Guid passoId, bool? ganhou)
    {
        var aposta = ApostaMultipla.Create(
            _usuarioId, bancaId, oddCombinada: 1.4m, stake: 25m, OrigemDaAposta.Alavancagem, descricao: null, passoDaJornadaId: passoId, DateTime.UtcNow);

        if (ganhou.HasValue)
        {
            aposta.Liquidar(ganhou.Value, DateTime.UtcNow);
        }

        return aposta;
    }

    private void ConfigurarRepositorios(
        PassoDaJornada passo, JornadaDeAlavancagem jornada, Banca banca, IReadOnlyCollection<ApostaMultipla> apostas)
    {
        _passoDaJornadaRepository.GetAsync(passo.Id, Arg.Any<CancellationToken>()).Returns(passo);
        _apostaMultiplaRepository.GetPorPassoDaJornadaAsync(passo.Id, Arg.Any<CancellationToken>()).Returns(apostas);
        _jornadaDeAlavancagemRepository.GetByIdAsync(jornada.Id, Arg.Any<CancellationToken>()).Returns(jornada);
        _bancaRepository.GetAsync(banca.Id, jornada.UsuarioId, Arg.Any<CancellationToken>()).Returns(banca);
    }

    [Fact]
    public async Task Handle_should_return_success_without_side_effects_when_passo_already_evaluated()
    {
        Banca banca = CriarBanca(200m);
        var jornada = JornadaDeAlavancagem.Create(
            _usuarioId, banca.Id, FaixaDeMeta.Dobrar, 75m, 150m, numeroDeFracoes: 3, totalDePassos: 3, 0.48m, DateTime.UtcNow);
        PassoDaJornada passo = CriarPasso(jornada.Id, numeroDeApostas: 3, StatusDoPasso.Avancou);

        _passoDaJornadaRepository.GetAsync(passo.Id, Arg.Any<CancellationToken>()).Returns(passo);

        Result resultado = await CriarHandler().Handle(new AvaliarPassoDaJornadaCommand(passo.Id), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        await _apostaMultiplaRepository.DidNotReceive().GetPorPassoDaJornadaAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_return_success_without_side_effects_when_some_aposta_still_pendente()
    {
        Banca banca = CriarBanca(200m);
        var jornada = JornadaDeAlavancagem.Create(
            _usuarioId, banca.Id, FaixaDeMeta.Dobrar, 75m, 150m, numeroDeFracoes: 3, totalDePassos: 3, 0.48m, DateTime.UtcNow);
        PassoDaJornada passo = CriarPasso(jornada.Id, numeroDeApostas: 3);

        ApostaMultipla aposta1 = CriarApostaLiquidada(banca.Id, passo.Id, ganhou: true);
        ApostaMultipla aposta2 = CriarApostaLiquidada(banca.Id, passo.Id, ganhou: null);

        _passoDaJornadaRepository.GetAsync(passo.Id, Arg.Any<CancellationToken>()).Returns(passo);
        _apostaMultiplaRepository.GetPorPassoDaJornadaAsync(passo.Id, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<ApostaMultipla>)[aposta1, aposta2]);

        Result resultado = await CriarHandler().Handle(new AvaliarPassoDaJornadaCommand(passo.Id), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        passo.Status.Should().Be(StatusDoPasso.EmAberto);
        await _jornadaDeAlavancagemRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_advance_passo_and_jornada_when_two_of_three_win_and_not_last_passo()
    {
        Banca banca = CriarBanca(200m);
        var jornada = JornadaDeAlavancagem.Create(
            _usuarioId, banca.Id, FaixaDeMeta.Dobrar, 75m, 150m, numeroDeFracoes: 3, totalDePassos: 3, 0.48m, DateTime.UtcNow);
        PassoDaJornada passo = CriarPasso(jornada.Id, numeroDeApostas: 3);

        ApostaMultipla a1 = CriarApostaLiquidada(banca.Id, passo.Id, true);
        ApostaMultipla a2 = CriarApostaLiquidada(banca.Id, passo.Id, true);
        ApostaMultipla a3 = CriarApostaLiquidada(banca.Id, passo.Id, false);

        ConfigurarRepositorios(passo, jornada, banca, [a1, a2, a3]);

        Result resultado = await CriarHandler().Handle(new AvaliarPassoDaJornadaCommand(passo.Id), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        passo.Status.Should().Be(StatusDoPasso.Avancou);
        passo.ValorResultante.Should().Be(200m);
        jornada.PassoAtual.Should().Be(2);
        jornada.Status.Should().Be(StatusDaJornada.EmAndamento);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_conclude_jornada_when_passo_avanca_on_last_passo()
    {
        Banca banca = CriarBanca(300m);
        var jornada = JornadaDeAlavancagem.Create(
            _usuarioId, banca.Id, FaixaDeMeta.Dobrar, 75m, 150m, numeroDeFracoes: 3, totalDePassos: 3, 0.48m, DateTime.UtcNow);
        jornada.AvancarPasso(DateTime.UtcNow);
        jornada.AvancarPasso(DateTime.UtcNow);
        PassoDaJornada passo = CriarPasso(jornada.Id, numeroDeApostas: 3);

        ApostaMultipla a1 = CriarApostaLiquidada(banca.Id, passo.Id, true);
        ApostaMultipla a2 = CriarApostaLiquidada(banca.Id, passo.Id, true);
        ApostaMultipla a3 = CriarApostaLiquidada(banca.Id, passo.Id, true);

        ConfigurarRepositorios(passo, jornada, banca, [a1, a2, a3]);

        Result resultado = await CriarHandler().Handle(new AvaliarPassoDaJornadaCommand(passo.Id), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        passo.Status.Should().Be(StatusDoPasso.Avancou);
        jornada.Status.Should().Be(StatusDaJornada.Concluida);
        jornada.PassoAtual.Should().Be(3);
    }

    [Fact]
    public async Task Handle_should_break_jornada_when_zero_apostas_win()
    {
        Banca banca = CriarBanca(0m);
        var jornada = JornadaDeAlavancagem.Create(
            _usuarioId, banca.Id, FaixaDeMeta.Dobrar, 75m, 150m, numeroDeFracoes: 3, totalDePassos: 3, 0.48m, DateTime.UtcNow);
        PassoDaJornada passo = CriarPasso(jornada.Id, numeroDeApostas: 3);

        ApostaMultipla a1 = CriarApostaLiquidada(banca.Id, passo.Id, false);
        ApostaMultipla a2 = CriarApostaLiquidada(banca.Id, passo.Id, false);
        ApostaMultipla a3 = CriarApostaLiquidada(banca.Id, passo.Id, false);

        ConfigurarRepositorios(passo, jornada, banca, [a1, a2, a3]);

        Result resultado = await CriarHandler().Handle(new AvaliarPassoDaJornadaCommand(passo.Id), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        passo.Status.Should().Be(StatusDoPasso.Quebrou);
        passo.ValorResultante.Should().Be(0m);
        jornada.Status.Should().Be(StatusDaJornada.Quebrada);
        jornada.PassoAtual.Should().Be(1);
    }

    [Fact]
    public async Task Handle_should_keep_jornada_em_andamento_when_one_win_of_four_and_valor_ainda_cobre_banca_minima()
    {
        // bancaMinima com 4 frações = 100 — 150 ainda cobre, então a jornada tenta o mesmo passo de novo.
        Banca banca = CriarBanca(150m);
        var jornada = JornadaDeAlavancagem.Create(
            _usuarioId, banca.Id, FaixaDeMeta.CincoVezes, 100m, 500m, numeroDeFracoes: 4, totalDePassos: 8, 0.49m, DateTime.UtcNow);
        PassoDaJornada passo = CriarPasso(jornada.Id, numeroDeApostas: 4);

        ApostaMultipla a1 = CriarApostaLiquidada(banca.Id, passo.Id, true);
        ApostaMultipla a2 = CriarApostaLiquidada(banca.Id, passo.Id, false);
        ApostaMultipla a3 = CriarApostaLiquidada(banca.Id, passo.Id, false);
        ApostaMultipla a4 = CriarApostaLiquidada(banca.Id, passo.Id, false);

        ConfigurarRepositorios(passo, jornada, banca, [a1, a2, a3, a4]);

        Result resultado = await CriarHandler().Handle(new AvaliarPassoDaJornadaCommand(passo.Id), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        passo.Status.Should().Be(StatusDoPasso.Quebrou);
        passo.ValorResultante.Should().Be(150m);
        jornada.Status.Should().Be(StatusDaJornada.EmAndamento);
        jornada.PassoAtual.Should().Be(1);
    }

    [Fact]
    public async Task Handle_should_break_jornada_when_one_win_of_four_but_valor_fica_abaixo_da_banca_minima()
    {
        // bancaMinima com 4 frações = 100 — 50 não cobre mais, a jornada encerra mesmo com 1 vitória.
        Banca banca = CriarBanca(50m);
        var jornada = JornadaDeAlavancagem.Create(
            _usuarioId, banca.Id, FaixaDeMeta.CincoVezes, 100m, 500m, numeroDeFracoes: 4, totalDePassos: 8, 0.49m, DateTime.UtcNow);
        PassoDaJornada passo = CriarPasso(jornada.Id, numeroDeApostas: 4);

        ApostaMultipla a1 = CriarApostaLiquidada(banca.Id, passo.Id, true);
        ApostaMultipla a2 = CriarApostaLiquidada(banca.Id, passo.Id, false);
        ApostaMultipla a3 = CriarApostaLiquidada(banca.Id, passo.Id, false);
        ApostaMultipla a4 = CriarApostaLiquidada(banca.Id, passo.Id, false);

        ConfigurarRepositorios(passo, jornada, banca, [a1, a2, a3, a4]);

        Result resultado = await CriarHandler().Handle(new AvaliarPassoDaJornadaCommand(passo.Id), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        passo.Status.Should().Be(StatusDoPasso.Quebrou);
        jornada.Status.Should().Be(StatusDaJornada.Quebrada);
    }

    [Fact]
    public async Task Handle_should_fail_when_passo_not_found()
    {
        var passoId = Guid.NewGuid();
        _passoDaJornadaRepository.GetAsync(passoId, Arg.Any<CancellationToken>()).Returns((PassoDaJornada?)null);

        Result resultado = await CriarHandler().Handle(new AvaliarPassoDaJornadaCommand(passoId), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(PassoDaJornadaErrors.NotFound(passoId));
    }

    [Fact]
    public async Task Handle_should_fail_when_jornada_not_found()
    {
        var jornadaId = Guid.NewGuid();
        PassoDaJornada passo = CriarPasso(jornadaId, numeroDeApostas: 3);

        ApostaMultipla a1 = CriarApostaLiquidada(Guid.NewGuid(), passo.Id, true);
        ApostaMultipla a2 = CriarApostaLiquidada(Guid.NewGuid(), passo.Id, true);
        ApostaMultipla a3 = CriarApostaLiquidada(Guid.NewGuid(), passo.Id, false);

        _passoDaJornadaRepository.GetAsync(passo.Id, Arg.Any<CancellationToken>()).Returns(passo);
        _apostaMultiplaRepository.GetPorPassoDaJornadaAsync(passo.Id, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<ApostaMultipla>)[a1, a2, a3]);
        _jornadaDeAlavancagemRepository.GetByIdAsync(jornadaId, Arg.Any<CancellationToken>()).Returns((JornadaDeAlavancagem?)null);

        Result resultado = await CriarHandler().Handle(new AvaliarPassoDaJornadaCommand(passo.Id), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(JornadaDeAlavancagemErrors.NotFound(jornadaId));
    }

    [Fact]
    public async Task Handle_should_fail_when_banca_not_found()
    {
        var bancaId = Guid.NewGuid();
        var jornada = JornadaDeAlavancagem.Create(
            _usuarioId, bancaId, FaixaDeMeta.Dobrar, 75m, 150m, numeroDeFracoes: 3, totalDePassos: 3, 0.48m, DateTime.UtcNow);
        PassoDaJornada passo = CriarPasso(jornada.Id, numeroDeApostas: 3);

        ApostaMultipla a1 = CriarApostaLiquidada(bancaId, passo.Id, true);
        ApostaMultipla a2 = CriarApostaLiquidada(bancaId, passo.Id, true);
        ApostaMultipla a3 = CriarApostaLiquidada(bancaId, passo.Id, false);

        _passoDaJornadaRepository.GetAsync(passo.Id, Arg.Any<CancellationToken>()).Returns(passo);
        _apostaMultiplaRepository.GetPorPassoDaJornadaAsync(passo.Id, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<ApostaMultipla>)[a1, a2, a3]);
        _jornadaDeAlavancagemRepository.GetByIdAsync(jornada.Id, Arg.Any<CancellationToken>()).Returns(jornada);
        _bancaRepository.GetAsync(bancaId, _usuarioId, Arg.Any<CancellationToken>()).Returns((Banca?)null);

        Result resultado = await CriarHandler().Handle(new AvaliarPassoDaJornadaCommand(passo.Id), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(BancaErrors.NotFound(bancaId));
    }
}
