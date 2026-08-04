using FluentAssertions;
using NSubstitute;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Application.Abstractions.Data;
using Oddify.Modules.Fixtures.Application.Partidas.RegistrarResultado;
using Oddify.Modules.Fixtures.Domain.Partidas;

namespace Oddify.UnitTests.Modules.Fixtures.Partidas;

public sealed class RegistrarResultadoCommandHandlerTests
{
    private readonly IPartidaRepository _partidaRepository = Substitute.For<IPartidaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private RegistrarResultadoCommandHandler CriarHandler() => new(_partidaRepository, _unitOfWork);

    [Fact]
    public async Task Handle_should_register_resultado_and_persist_when_partida_exists()
    {
        var partida = Partida.Create("partida-1", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, rodada: 1, temporada: 2026);
        _partidaRepository.GetAsync(partida.Id, Arg.Any<CancellationToken>()).Returns(partida);

        var command = new RegistrarResultadoCommand(partida.Id, 2, 1);

        Result resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        partida.Situacao.Should().Be(SituacaoDaPartida.Encerrada);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_return_failure_when_partida_not_found()
    {
        var partidaId = Guid.NewGuid();
        _partidaRepository.GetAsync(partidaId, Arg.Any<CancellationToken>()).Returns((Partida?)null);

        Result resultado = await CriarHandler().Handle(new RegistrarResultadoCommand(partidaId, 1, 0), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(PartidaErrors.NotFound(partidaId));
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_return_failure_when_partida_already_encerrada()
    {
        var partida = Partida.Create("partida-1", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, rodada: 1, temporada: 2026);
        partida.RegistrarResultado(1, 1);
        _partidaRepository.GetAsync(partida.Id, Arg.Any<CancellationToken>()).Returns(partida);

        Result resultado = await CriarHandler().Handle(new RegistrarResultadoCommand(partida.Id, 2, 0), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(PartidaErrors.JaEncerrada(partida.Id));
    }
}
