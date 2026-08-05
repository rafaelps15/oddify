using FluentAssertions;
using NSubstitute;
using Oddify.Common.Application.Authentication;
using Oddify.Common.Application.Clock;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.Abstractions.Data;
using Oddify.Modules.Apostas.Application.Bancas.DepositarNaBanca;
using Oddify.Modules.Apostas.Domain.Bancas;
using Oddify.Modules.Apostas.Domain.MovimentacoesDaBanca;

namespace Oddify.UnitTests.Modules.Apostas.DepositarNaBanca;

public sealed class DepositarNaBancaCommandHandlerTests
{
    private readonly IBancaRepository _bancaRepository = Substitute.For<IBancaRepository>();
    private readonly IMovimentacaoDaBancaRepository _movimentacaoDaBancaRepository = Substitute.For<IMovimentacaoDaBancaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly Guid _usuarioId = Guid.NewGuid();

    private Banca CriarBanca(decimal saldoInicial) =>
        Banca.Create(_usuarioId, "Banca principal", saldoInicial, 0.05m, modoPaperTrading: true, DateTime.UtcNow);

    private DepositarNaBancaCommandHandler CriarHandler()
    {
        _userContext.UserId.Returns(_usuarioId);
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);
        return new(_bancaRepository, _movimentacaoDaBancaRepository, _unitOfWork, _userContext, _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_should_credit_saldo_and_insert_movimentacao_de_deposito()
    {
        Banca banca = CriarBanca(1000m);
        _bancaRepository.GetAsync(banca.Id, _usuarioId, Arg.Any<CancellationToken>()).Returns(banca);

        Result resultado = await CriarHandler().Handle(new DepositarNaBancaCommand(banca.Id, 250m), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        banca.SaldoAtual.Should().Be(1250m);
        _movimentacaoDaBancaRepository.Received(1).Insert(Arg.Is<MovimentacaoDaBanca>(m =>
            m.BancaId == banca.Id
            && m.Tipo == TipoDeMovimentacao.Deposito
            && m.Valor == 250m
            && m.SaldoAposMovimentacao == 1250m
            && m.ApostaMultiplaId == null));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_fail_when_banca_not_found()
    {
        var bancaId = Guid.NewGuid();
        _bancaRepository.GetAsync(bancaId, _usuarioId, Arg.Any<CancellationToken>()).Returns((Banca?)null);

        Result resultado = await CriarHandler().Handle(new DepositarNaBancaCommand(bancaId, 250m), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(BancaErrors.NotFound(bancaId));
        _movimentacaoDaBancaRepository.DidNotReceive().Insert(Arg.Any<MovimentacaoDaBanca>());
    }
}
