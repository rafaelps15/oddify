using FluentAssertions;
using NSubstitute;
using Oddify.Common.Domain;
using Oddify.Modules.Analise.Application.Abstractions.Analises;
using Oddify.Modules.Analise.Application.Abstractions.Data;
using Oddify.Modules.Analise.Application.Analises.AnalisarPartida;
using Oddify.Modules.Analise.Application.Calculo;
using Oddify.Modules.Analise.Domain.Analises;

namespace Oddify.UnitTests.Modules.Analise.AnalisarPartida;

public sealed class AnalisarPartidaCommandHandlerTests
{
    private readonly IAnaliseDePartidaDadosService _dadosService = Substitute.For<IAnaliseDePartidaDadosService>();
    private readonly IAnaliseDePartidaRepository _analiseRepository = Substitute.For<IAnaliseDePartidaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly Guid _partidaId = Guid.NewGuid();

    private AnalisarPartidaCommandHandler CriarHandler() => new(_dadosService, _analiseRepository, _unitOfWork);

    [Fact]
    public async Task Handle_should_create_and_persist_analise_when_dados_are_available()
    {
        var calculo = new AnaliseCalculada(
            ProbPoissonPura: 0.642m,
            ProbDixonColes: 0.65m,
            ProbImplicitaDaOdd: 0.5m,
            Vantagem: 0.15m,
            Odd: 2.0m,
            Aprovada: true,
            Motivo: null);

        _dadosService.ObterAsync(_partidaId, "vitoria_casa", Arg.Any<CancellationToken>()).Returns(calculo);

        var command = new AnalisarPartidaCommand(_partidaId, "vitoria_casa");

        Result<Guid> resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();

        _analiseRepository.Received(1).Insert(Arg.Is<AnaliseDePartida>(a =>
            a.PartidaId == _partidaId &&
            a.Mercado == "vitoria_casa" &&
            a.AprovadaNoFiltro));

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_return_failure_and_not_persist_when_dados_are_unavailable()
    {
        _dadosService.ObterAsync(_partidaId, "vitoria_casa", Arg.Any<CancellationToken>()).Returns((AnaliseCalculada?)null);

        var command = new AnalisarPartidaCommand(_partidaId, "vitoria_casa");

        Result<Guid> resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Code.Should().Be("Analises.DadosIndisponiveis");
        _analiseRepository.DidNotReceive().Insert(Arg.Any<AnaliseDePartida>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
