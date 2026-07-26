using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Oddify.Common.Domain;
using Oddify.Modules.Analise.Application.Abstractions.Data;
using Oddify.Modules.Analise.Application.Abstractions.Llm;
using Oddify.Modules.Analise.Application.Analises.AvaliarComClaude;
using Oddify.Modules.Analise.Domain.Analises;

namespace Oddify.UnitTests.Modules.Analise.AvaliarComClaude;

public sealed class AvaliarComClaudeCommandHandlerTests
{
    private readonly IAnaliseDePartidaRepository _analiseRepository = Substitute.For<IAnaliseDePartidaRepository>();
    private readonly IClaudeAvaliadorCriticoService _claudeService = Substitute.For<IClaudeAvaliadorCriticoService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    // NullLogger em vez de um substitute: NSubstitute não consegue gerar um proxy para
    // ILogger<T> quando T é um tipo internal de outro assembly forte-assinado (Microsoft.Extensions.Logging.Abstractions).
    private readonly ILogger<AvaliarComClaudeCommandHandler> _logger = NullLogger<AvaliarComClaudeCommandHandler>.Instance;

    private AvaliarComClaudeCommandHandler CriarHandler() => new(_analiseRepository, _claudeService, _unitOfWork, _logger);

    private static AnaliseDePartida CriarAnaliseAprovada() =>
        AnaliseDePartida.Create(Guid.NewGuid(), "vitoria_casa", 0.55m, 0.55m, 0.5m, 0.05m, 1.5m, aprovadaNoFiltro: true, null, DateTime.UtcNow);

    [Fact]
    public async Task Handle_should_return_failure_when_analise_not_found()
    {
        var analiseId = Guid.NewGuid();
        _analiseRepository.GetAsync(analiseId, Arg.Any<CancellationToken>()).Returns((AnaliseDePartida?)null);

        Result resultado = await CriarHandler().Handle(new AvaliarComClaudeCommand(analiseId, null), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(AnaliseDePartidaErrors.NotFound(analiseId));
    }

    [Fact]
    public async Task Handle_should_return_failure_when_analise_not_aprovada_no_filtro()
    {
        var analise = AnaliseDePartida.Create(Guid.NewGuid(), "vitoria_casa", 0.5m, 0.5m, 0.5m, 0.0m, 1.5m, aprovadaNoFiltro: false, "motivo", DateTime.UtcNow);
        _analiseRepository.GetAsync(analise.Id, Arg.Any<CancellationToken>()).Returns(analise);

        Result resultado = await CriarHandler().Handle(new AvaliarComClaudeCommand(analise.Id, null), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(AnaliseDePartidaErrors.NaoAprovadaNoFiltro(analise.Id));
    }

    [Fact]
    public async Task Handle_should_register_decision_and_save_when_claude_succeeds()
    {
        AnaliseDePartida analise = CriarAnaliseAprovada();
        _analiseRepository.GetAsync(analise.Id, Arg.Any<CancellationToken>()).Returns(analise);

        _claudeService.AvaliarAsync(Arg.Any<AnaliseContexto>(), Arg.Any<CancellationToken>())
            .Returns(new VeredictoClaude(DecisaoDoClaude.Confirma, "vantagem consistente", "{\"decisao\":\"CONFIRMA\"}"));

        Result resultado = await CriarHandler().Handle(new AvaliarComClaudeCommand(analise.Id, null), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        analise.DecisaoDoClaude.Should().Be(DecisaoDoClaude.Confirma);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_return_success_and_keep_decisao_naoavaliada_when_claude_service_fails()
    {
        AnaliseDePartida analise = CriarAnaliseAprovada();
        _analiseRepository.GetAsync(analise.Id, Arg.Any<CancellationToken>()).Returns(analise);

        _claudeService.AvaliarAsync(Arg.Any<AnaliseContexto>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<VeredictoClaude>(Error.Failure("Analises.ErroNaChamadaAoClaude", "timeout")));

        Result resultado = await CriarHandler().Handle(new AvaliarComClaudeCommand(analise.Id, null), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        analise.DecisaoDoClaude.Should().Be(DecisaoDoClaude.NaoAvaliada);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
