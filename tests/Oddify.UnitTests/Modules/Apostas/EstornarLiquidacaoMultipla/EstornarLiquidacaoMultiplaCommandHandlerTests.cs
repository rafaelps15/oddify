using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Oddify.Common.Application.Authentication;
using Oddify.Common.Application.Clock;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.Abstractions.Data;
using Oddify.Modules.Apostas.Application.ApostasMultiplas.EstornarLiquidacaoMultipla;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;
using Oddify.Modules.Apostas.Domain.Bancas;
using Oddify.Modules.Apostas.Domain.MovimentacoesDaBanca;
using Oddify.Modules.Apostas.Domain.PernasDeAposta;

namespace Oddify.UnitTests.Modules.Apostas.EstornarLiquidacaoMultipla;

public sealed class EstornarLiquidacaoMultiplaCommandHandlerTests
{
    private readonly IApostaMultiplaRepository _apostaMultiplaRepository = Substitute.For<IApostaMultiplaRepository>();
    private readonly IPernaDeApostaRepository _pernaDeApostaRepository = Substitute.For<IPernaDeApostaRepository>();
    private readonly IBancaRepository _bancaRepository = Substitute.For<IBancaRepository>();
    private readonly IMovimentacaoDaBancaRepository _movimentacaoDaBancaRepository = Substitute.For<IMovimentacaoDaBancaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly Guid _usuarioId = Guid.NewGuid();

    private Banca CriarBanca(decimal saldoInicial) =>
        Banca.Create(_usuarioId, "Banca principal", saldoInicial, 0.05m, PerfilDeRisco.Moderado, modoPaperTrading: true, FinalidadeDaBanca.Principal, DateTime.UtcNow);

    private EstornarLiquidacaoMultiplaCommandHandler CriarHandler()
    {
        _userContext.UserId.Returns(_usuarioId);
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);
        return new(_apostaMultiplaRepository, _pernaDeApostaRepository, _bancaRepository, _movimentacaoDaBancaRepository, _unitOfWork, _dateTimeProvider, _userContext);
    }

    [Fact]
    public async Task Handle_should_return_multipla_to_pendente_and_reverse_saldo()
    {
        Banca banca = CriarBanca(1000m);
        var apostaMultipla = ApostaMultipla.Create(
            _usuarioId, banca.Id, oddCombinada: 4.0m, stake: 50m, OrigemDaAposta.ManualEntry, descricao: null, passoDaJornadaId: null, DateTime.UtcNow);
        apostaMultipla.Liquidar(ganhou: true, DateTime.UtcNow);
        banca.RegistrarMovimentacao(apostaMultipla.LucroOuPerda!.Value, TipoDeMovimentacao.Liquidacao, apostaMultipla.Id, DateTime.UtcNow);

        var perna = PernaDeAposta.Create(apostaMultipla.Id, Guid.NewGuid(), Guid.NewGuid(), "vitoria_casa", 4.0m);
        perna.Resolver(true);

        _apostaMultiplaRepository.GetAsync(apostaMultipla.Id, _usuarioId, Arg.Any<CancellationToken>()).Returns(apostaMultipla);
        _pernaDeApostaRepository.GetPorApostaMultiplaAsync(apostaMultipla.Id, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<PernaDeAposta>)[perna]);
        _bancaRepository.GetAsync(banca.Id, _usuarioId, Arg.Any<CancellationToken>()).Returns(banca);

        Result resultado = await CriarHandler().Handle(new EstornarLiquidacaoMultiplaCommand(apostaMultipla.Id), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        apostaMultipla.Resultado.Should().Be(ResultadoDaAposta.Pendente);
        apostaMultipla.LucroOuPerda.Should().BeNull();
        perna.Resultado.Should().Be(ResultadoDaAposta.Pendente);
        banca.SaldoAtual.Should().Be(1000m);
        _movimentacaoDaBancaRepository.Received(1).Insert(Arg.Is<MovimentacaoDaBanca>(m =>
            m.BancaId == banca.Id
            && m.Tipo == TipoDeMovimentacao.Estorno
            && m.Valor == -(50m * (4.0m - 1m))
            && m.SaldoAposMovimentacao == banca.SaldoAtual
            && m.ApostaMultiplaId == apostaMultipla.Id));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_fail_when_ainda_pendente()
    {
        Banca banca = CriarBanca(1000m);
        var apostaMultipla = ApostaMultipla.Create(
            _usuarioId, banca.Id, oddCombinada: 4.0m, stake: 50m, OrigemDaAposta.ManualEntry, descricao: null, passoDaJornadaId: null, DateTime.UtcNow);

        _apostaMultiplaRepository.GetAsync(apostaMultipla.Id, _usuarioId, Arg.Any<CancellationToken>()).Returns(apostaMultipla);

        Result resultado = await CriarHandler().Handle(new EstornarLiquidacaoMultiplaCommand(apostaMultipla.Id), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(ApostaMultiplaErrors.AindaNaoLiquidada(apostaMultipla.Id));
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_fail_when_aposta_not_found()
    {
        var apostaMultiplaId = Guid.NewGuid();
        _apostaMultiplaRepository.GetAsync(apostaMultiplaId, _usuarioId, Arg.Any<CancellationToken>()).Returns((ApostaMultipla?)null);

        Result resultado = await CriarHandler().Handle(new EstornarLiquidacaoMultiplaCommand(apostaMultiplaId), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(ApostaMultiplaErrors.NotFound(apostaMultiplaId));
    }

    [Fact]
    public async Task Handle_should_return_conflito_de_concorrencia_when_save_throws_concurrency_exception()
    {
        Banca banca = CriarBanca(1000m);
        var apostaMultipla = ApostaMultipla.Create(
            _usuarioId, banca.Id, oddCombinada: 4.0m, stake: 50m, OrigemDaAposta.ManualEntry, descricao: null, passoDaJornadaId: null, DateTime.UtcNow);
        apostaMultipla.Liquidar(ganhou: true, DateTime.UtcNow);
        banca.RegistrarMovimentacao(apostaMultipla.LucroOuPerda!.Value, TipoDeMovimentacao.Liquidacao, apostaMultipla.Id, DateTime.UtcNow);

        var perna = PernaDeAposta.Create(apostaMultipla.Id, Guid.NewGuid(), Guid.NewGuid(), "vitoria_casa", 4.0m);
        perna.Resolver(true);

        _apostaMultiplaRepository.GetAsync(apostaMultipla.Id, _usuarioId, Arg.Any<CancellationToken>()).Returns(apostaMultipla);
        _pernaDeApostaRepository.GetPorApostaMultiplaAsync(apostaMultipla.Id, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<PernaDeAposta>)[perna]);
        _bancaRepository.GetAsync(banca.Id, _usuarioId, Arg.Any<CancellationToken>()).Returns(banca);

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new DbUpdateConcurrencyException("conflito simulado"));

        Result resultado = await CriarHandler().Handle(new EstornarLiquidacaoMultiplaCommand(apostaMultipla.Id), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(CommonErrors.ConflitoDeConcorrencia);
    }
}
