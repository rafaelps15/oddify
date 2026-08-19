using FluentAssertions;
using NSubstitute;
using Oddify.Common.Domain;
using Oddify.Modules.Analise.Application.Abstractions.Data;
using Oddify.Modules.Analise.Application.Fixtures.RegistrarPartida;
using Oddify.Modules.Analise.Domain.Fixtures;

namespace Oddify.UnitTests.Modules.Analise.Fixtures;

public sealed class RegistrarPartidaCommandHandlerTests
{
    private readonly IPartidaRepository _partidaRepository = Substitute.For<IPartidaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private RegistrarPartidaCommandHandler CriarHandler() => new(_partidaRepository, _unitOfWork);

    [Fact]
    public async Task Handle_should_insert_when_partida_does_not_exist_yet()
    {
        var partidaId = Guid.NewGuid();
        var ligaId = Guid.NewGuid();
        var equipeCasaId = Guid.NewGuid();
        var equipeVisitanteId = Guid.NewGuid();
        DateTime dataUtc = DateTime.UtcNow;

        _partidaRepository.GetAsync(partidaId, Arg.Any<CancellationToken>()).Returns((Partida?)null);

        var command = new RegistrarPartidaCommand(partidaId, ligaId, equipeCasaId, equipeVisitanteId, dataUtc);

        Result resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        _partidaRepository.Received(1).Insert(Arg.Is<Partida>(p =>
            p.Id == partidaId && p.LigaId == ligaId && p.EquipeCasaId == equipeCasaId && p.EquipeVisitanteId == equipeVisitanteId));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_be_idempotent_when_partida_already_exists()
    {
        var partidaId = Guid.NewGuid();
        var partidaExistente = Partida.Create(partidaId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        _partidaRepository.GetAsync(partidaId, Arg.Any<CancellationToken>()).Returns(partidaExistente);

        var command = new RegistrarPartidaCommand(partidaId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

        Result resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        _partidaRepository.DidNotReceive().Insert(Arg.Any<Partida>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
