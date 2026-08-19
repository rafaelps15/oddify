using FluentAssertions;
using NSubstitute;
using Oddify.Common.Domain;
using Oddify.Modules.Analise.Application.Abstractions.Data;
using Oddify.Modules.Analise.Application.Fixtures.RegistrarResultadoDaPartida;
using Oddify.Modules.Analise.Domain.Fixtures;

namespace Oddify.UnitTests.Modules.Analise.Fixtures;

public sealed class RegistrarResultadoDaPartidaCommandHandlerTests
{
    private readonly IPartidaRepository _partidaRepository = Substitute.For<IPartidaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private RegistrarResultadoDaPartidaCommandHandler CriarHandler() => new(_partidaRepository, _unitOfWork);

    [Fact]
    public async Task Handle_should_set_score_when_partida_exists()
    {
        var partidaId = Guid.NewGuid();
        var partida = Partida.Create(partidaId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        _partidaRepository.GetAsync(partidaId, Arg.Any<CancellationToken>()).Returns(partida);

        var command = new RegistrarResultadoDaPartidaCommand(partidaId, GolsCasa: 2, GolsVisitante: 1);

        Result resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        partida.GolsCasa.Should().Be(2);
        partida.GolsVisitante.Should().Be(1);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_fail_when_partida_is_not_mirrored_yet()
    {
        var partidaId = Guid.NewGuid();
        _partidaRepository.GetAsync(partidaId, Arg.Any<CancellationToken>()).Returns((Partida?)null);

        var command = new RegistrarResultadoDaPartidaCommand(partidaId, GolsCasa: 2, GolsVisitante: 1);

        Result resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Code.Should().Be("Partidas.NotFound");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
