using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Oddify.Common.Application.Authentication;
using Oddify.Common.Application.Clock;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.Abstractions.Data;
using Oddify.Modules.Apostas.Application.ApostasMultiplas.ExcluirApostaMultipla;
using Oddify.Modules.Apostas.Domain.AnalisesDisponiveis;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;
using Oddify.Modules.Apostas.Domain.Bancas;
using Oddify.Modules.Apostas.Domain.MovimentacoesDaBanca;
using Oddify.Modules.Apostas.Domain.PernasDeAposta;

namespace Oddify.UnitTests.Modules.Apostas.ExcluirApostaMultipla;

public sealed class ExcluirApostaMultiplaCommandHandlerTests
{
    private readonly IApostaMultiplaRepository _apostaMultiplaRepository = Substitute.For<IApostaMultiplaRepository>();
    private readonly IPernaDeApostaRepository _pernaDeApostaRepository = Substitute.For<IPernaDeApostaRepository>();
    private readonly IAnaliseDisponivelParaApostaRepository _analiseDisponivelRepository = Substitute.For<IAnaliseDisponivelParaApostaRepository>();
    private readonly IBancaRepository _bancaRepository = Substitute.For<IBancaRepository>();
    private readonly IMovimentacaoDaBancaRepository _movimentacaoDaBancaRepository = Substitute.For<IMovimentacaoDaBancaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly Guid _usuarioId = Guid.NewGuid();

    private Banca CriarBanca(decimal saldoInicial) =>
        Banca.Create(_usuarioId, "Banca principal", saldoInicial, 0.05m, PerfilDeRisco.Moderado, modoPaperTrading: true, FinalidadeDaBanca.Principal, DateTime.UtcNow);

    private static AnaliseDisponivelParaAposta CriarDisponivelUtilizada()
    {
        var disponivel =
            AnaliseDisponivelParaAposta.Create(Guid.NewGuid(), Guid.NewGuid(), "vitoria_casa", 2.0m, 0.6m, reduzida: false);
        disponivel.MarcarComoUtilizada();
        return disponivel;
    }

    private ExcluirApostaMultiplaCommandHandler CriarHandler()
    {
        _userContext.UserId.Returns(_usuarioId);
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);
        return new(
            _apostaMultiplaRepository, _pernaDeApostaRepository, _analiseDisponivelRepository, _bancaRepository,
            _movimentacaoDaBancaRepository, _unitOfWork, _dateTimeProvider, _userContext);
    }

    [Fact]
    public async Task Handle_should_delete_pendente_aposta_and_liberar_analises_without_touching_saldo()
    {
        Banca banca = CriarBanca(1000m);
        var apostaMultipla = ApostaMultipla.Create(
            _usuarioId, banca.Id, oddCombinada: 4.0m, stake: 50m, OrigemDaAposta.ManualEntry, descricao: null, passoDaJornadaId: null, DateTime.UtcNow);

        AnaliseDisponivelParaAposta disponivel = CriarDisponivelUtilizada();
        var perna = PernaDeAposta.Create(apostaMultipla.Id, disponivel.Id, Guid.NewGuid(), "vitoria_casa", 4.0m);

        _apostaMultiplaRepository.GetAsync(apostaMultipla.Id, _usuarioId, Arg.Any<CancellationToken>()).Returns(apostaMultipla);
        _pernaDeApostaRepository.GetPorApostaMultiplaAsync(apostaMultipla.Id, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<PernaDeAposta>)[perna]);
        _analiseDisponivelRepository.GetAsync(disponivel.Id, Arg.Any<CancellationToken>()).Returns(disponivel);

        Result resultado = await CriarHandler().Handle(new ExcluirApostaMultiplaCommand(apostaMultipla.Id), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        disponivel.JaUtilizada.Should().BeFalse();
        banca.SaldoAtual.Should().Be(1000m);
        _apostaMultiplaRepository.Received(1).Delete(apostaMultipla);
        _movimentacaoDaBancaRepository.DidNotReceive().Insert(Arg.Any<MovimentacaoDaBanca>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_reverse_saldo_and_delete_when_aposta_ja_liquidada()
    {
        Banca banca = CriarBanca(1000m);
        var apostaMultipla = ApostaMultipla.Create(
            _usuarioId, banca.Id, oddCombinada: 4.0m, stake: 50m, OrigemDaAposta.ManualEntry, descricao: null, passoDaJornadaId: null, DateTime.UtcNow);
        apostaMultipla.Liquidar(ganhou: true, DateTime.UtcNow);
        banca.RegistrarMovimentacao(apostaMultipla.LucroOuPerda!.Value, TipoDeMovimentacao.Liquidacao, apostaMultipla.Id, DateTime.UtcNow);

        AnaliseDisponivelParaAposta disponivel = CriarDisponivelUtilizada();
        var perna = PernaDeAposta.Create(apostaMultipla.Id, disponivel.Id, Guid.NewGuid(), "vitoria_casa", 4.0m);
        perna.Resolver(true);

        _apostaMultiplaRepository.GetAsync(apostaMultipla.Id, _usuarioId, Arg.Any<CancellationToken>()).Returns(apostaMultipla);
        _pernaDeApostaRepository.GetPorApostaMultiplaAsync(apostaMultipla.Id, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<PernaDeAposta>)[perna]);
        _analiseDisponivelRepository.GetAsync(disponivel.Id, Arg.Any<CancellationToken>()).Returns(disponivel);
        _bancaRepository.GetAsync(banca.Id, _usuarioId, Arg.Any<CancellationToken>()).Returns(banca);

        Result resultado = await CriarHandler().Handle(new ExcluirApostaMultiplaCommand(apostaMultipla.Id), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        banca.SaldoAtual.Should().Be(1000m);
        disponivel.JaUtilizada.Should().BeFalse();
        _movimentacaoDaBancaRepository.Received(1).Insert(Arg.Is<MovimentacaoDaBanca>(m =>
            m.Tipo == TipoDeMovimentacao.Estorno && m.Valor == -(50m * (4.0m - 1m))));
        _apostaMultiplaRepository.Received(1).Delete(apostaMultipla);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_fail_when_aposta_not_found()
    {
        var apostaMultiplaId = Guid.NewGuid();
        _apostaMultiplaRepository.GetAsync(apostaMultiplaId, _usuarioId, Arg.Any<CancellationToken>()).Returns((ApostaMultipla?)null);

        Result resultado = await CriarHandler().Handle(new ExcluirApostaMultiplaCommand(apostaMultiplaId), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(ApostaMultiplaErrors.NotFound(apostaMultiplaId));
    }

    [Fact]
    public async Task Handle_should_fail_when_analise_not_found()
    {
        Banca banca = CriarBanca(1000m);
        var apostaMultipla = ApostaMultipla.Create(
            _usuarioId, banca.Id, oddCombinada: 4.0m, stake: 50m, OrigemDaAposta.ManualEntry, descricao: null, passoDaJornadaId: null, DateTime.UtcNow);

        var perna = PernaDeAposta.Create(apostaMultipla.Id, Guid.NewGuid(), Guid.NewGuid(), "vitoria_casa", 4.0m);

        _apostaMultiplaRepository.GetAsync(apostaMultipla.Id, _usuarioId, Arg.Any<CancellationToken>()).Returns(apostaMultipla);
        _pernaDeApostaRepository.GetPorApostaMultiplaAsync(apostaMultipla.Id, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<PernaDeAposta>)[perna]);
        _analiseDisponivelRepository.GetAsync(perna.AnaliseId, Arg.Any<CancellationToken>()).Returns((AnaliseDisponivelParaAposta?)null);

        Result resultado = await CriarHandler().Handle(new ExcluirApostaMultiplaCommand(apostaMultipla.Id), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(AnaliseDisponivelParaApostaErrors.NotFound(perna.AnaliseId));
        _apostaMultiplaRepository.DidNotReceive().Delete(Arg.Any<ApostaMultipla>());
    }

    [Fact]
    public async Task Handle_should_return_conflito_de_concorrencia_when_save_throws_concurrency_exception()
    {
        Banca banca = CriarBanca(1000m);
        var apostaMultipla = ApostaMultipla.Create(
            _usuarioId, banca.Id, oddCombinada: 4.0m, stake: 50m, OrigemDaAposta.ManualEntry, descricao: null, passoDaJornadaId: null, DateTime.UtcNow);

        AnaliseDisponivelParaAposta disponivel = CriarDisponivelUtilizada();
        var perna = PernaDeAposta.Create(apostaMultipla.Id, disponivel.Id, Guid.NewGuid(), "vitoria_casa", 4.0m);

        _apostaMultiplaRepository.GetAsync(apostaMultipla.Id, _usuarioId, Arg.Any<CancellationToken>()).Returns(apostaMultipla);
        _pernaDeApostaRepository.GetPorApostaMultiplaAsync(apostaMultipla.Id, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<PernaDeAposta>)[perna]);
        _analiseDisponivelRepository.GetAsync(disponivel.Id, Arg.Any<CancellationToken>()).Returns(disponivel);

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new DbUpdateConcurrencyException("conflito simulado"));

        Result resultado = await CriarHandler().Handle(new ExcluirApostaMultiplaCommand(apostaMultipla.Id), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(CommonErrors.ConflitoDeConcorrencia);
    }
}
