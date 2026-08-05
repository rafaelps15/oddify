using FluentAssertions;
using NSubstitute;
using Oddify.Common.Application.Authentication;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.Abstractions.Data;
using Oddify.Modules.Apostas.Application.ApostasMultiplas.MontarMultipla;
using Oddify.Modules.Apostas.Domain.AnalisesDisponiveis;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;
using Oddify.Modules.Apostas.Domain.Bancas;
using Oddify.Modules.Apostas.Domain.PernasDeAposta;

namespace Oddify.UnitTests.Modules.Apostas.MontarMultipla;

public sealed class MontarMultiplaCommandHandlerTests
{
    private readonly IBancaRepository _bancaRepository = Substitute.For<IBancaRepository>();
    private readonly IAnaliseDisponivelParaApostaRepository _analiseDisponivelRepository = Substitute.For<IAnaliseDisponivelParaApostaRepository>();
    private readonly IApostaMultiplaRepository _apostaMultiplaRepository = Substitute.For<IApostaMultiplaRepository>();
    private readonly IPernaDeApostaRepository _pernaDeApostaRepository = Substitute.For<IPernaDeApostaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly Guid _usuarioId = Guid.NewGuid();

    private MontarMultiplaCommandHandler CriarHandler()
    {
        _userContext.UserId.Returns(_usuarioId);
        return new(_bancaRepository, _analiseDisponivelRepository, _apostaMultiplaRepository, _pernaDeApostaRepository, _unitOfWork, _userContext);
    }

    private Banca CriarBanca(decimal saldoInicial) =>
        Banca.Create(_usuarioId, "Banca principal", saldoInicial, 0.05m, modoPaperTrading: true, DateTime.UtcNow);

    private static AnaliseDisponivelParaAposta CriarDisponivel(Guid partidaId, decimal odd, decimal probabilidade, bool reduzida = false) =>
        AnaliseDisponivelParaAposta.Create(Guid.NewGuid(), partidaId, "vitoria_casa", odd, probabilidade, reduzida);

    [Fact]
    public async Task Handle_should_create_multipla_when_two_analises_de_partidas_diferentes_com_vantagem()
    {
        Banca banca = CriarBanca(1000m);
        _bancaRepository.GetAsync(banca.Id, _usuarioId, Arg.Any<CancellationToken>()).Returns(banca);

        AnaliseDisponivelParaAposta disponivel1 = CriarDisponivel(Guid.NewGuid(), odd: 2.0m, probabilidade: 0.60m);
        AnaliseDisponivelParaAposta disponivel2 = CriarDisponivel(Guid.NewGuid(), odd: 2.0m, probabilidade: 0.60m);

        _analiseDisponivelRepository.GetAsync(disponivel1.Id, Arg.Any<CancellationToken>()).Returns(disponivel1);
        _analiseDisponivelRepository.GetAsync(disponivel2.Id, Arg.Any<CancellationToken>()).Returns(disponivel2);

        var command = new MontarMultiplaCommand(banca.Id, [disponivel1.Id, disponivel2.Id]);

        Result<Guid> resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        _apostaMultiplaRepository.Received(1).Insert(Arg.Is<ApostaMultipla>(a => a.OddCombinada == 4.0m));
        _pernaDeApostaRepository.Received(2).Insert(Arg.Any<PernaDeAposta>());
        disponivel1.JaUtilizada.Should().BeTrue();
        disponivel2.JaUtilizada.Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_fail_with_partidas_repetidas_when_same_partida_used_twice()
    {
        Banca banca = CriarBanca(1000m);
        _bancaRepository.GetAsync(banca.Id, _usuarioId, Arg.Any<CancellationToken>()).Returns(banca);

        var partidaId = Guid.NewGuid();
        AnaliseDisponivelParaAposta disponivel1 = CriarDisponivel(partidaId, odd: 2.0m, probabilidade: 0.60m);
        AnaliseDisponivelParaAposta disponivel2 = CriarDisponivel(partidaId, odd: 1.8m, probabilidade: 0.60m);

        _analiseDisponivelRepository.GetAsync(disponivel1.Id, Arg.Any<CancellationToken>()).Returns(disponivel1);
        _analiseDisponivelRepository.GetAsync(disponivel2.Id, Arg.Any<CancellationToken>()).Returns(disponivel2);

        var command = new MontarMultiplaCommand(banca.Id, [disponivel1.Id, disponivel2.Id]);

        Result<Guid> resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(ApostaMultiplaErrors.PartidasRepetidas);
        _apostaMultiplaRepository.DidNotReceive().Insert(Arg.Any<ApostaMultipla>());
    }

    [Fact]
    public async Task Handle_should_fail_with_stake_nulo_when_there_is_no_edge()
    {
        Banca banca = CriarBanca(1000m);
        _bancaRepository.GetAsync(banca.Id, _usuarioId, Arg.Any<CancellationToken>()).Returns(banca);

        // probabilidade * odd == 1 para cada perna -> combinada também sem vantagem -> stake zero
        AnaliseDisponivelParaAposta disponivel1 = CriarDisponivel(Guid.NewGuid(), odd: 2.0m, probabilidade: 0.5m);
        AnaliseDisponivelParaAposta disponivel2 = CriarDisponivel(Guid.NewGuid(), odd: 2.0m, probabilidade: 0.5m);

        _analiseDisponivelRepository.GetAsync(disponivel1.Id, Arg.Any<CancellationToken>()).Returns(disponivel1);
        _analiseDisponivelRepository.GetAsync(disponivel2.Id, Arg.Any<CancellationToken>()).Returns(disponivel2);

        var command = new MontarMultiplaCommand(banca.Id, [disponivel1.Id, disponivel2.Id]);

        Result<Guid> resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(ApostaMultiplaErrors.StakeNulo);
    }

    [Fact]
    public async Task Handle_should_fail_when_banca_not_found()
    {
        var bancaId = Guid.NewGuid();
        _bancaRepository.GetAsync(bancaId, _usuarioId, Arg.Any<CancellationToken>()).Returns((Banca?)null);

        var command = new MontarMultiplaCommand(bancaId, [Guid.NewGuid(), Guid.NewGuid()]);

        Result<Guid> resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(BancaErrors.NotFound(bancaId));
    }
}
