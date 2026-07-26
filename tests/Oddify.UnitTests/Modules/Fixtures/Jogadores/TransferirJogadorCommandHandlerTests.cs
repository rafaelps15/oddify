using FluentAssertions;
using NSubstitute;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Application.Abstractions.Data;
using Oddify.Modules.Fixtures.Application.Jogadores.TransferirJogador;
using Oddify.Modules.Fixtures.Domain.Jogadores;

namespace Oddify.UnitTests.Modules.Fixtures.Jogadores;

public sealed class TransferirJogadorCommandHandlerTests
{
    private readonly IJogadorRepository _jogadorRepository = Substitute.For<IJogadorRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private TransferirJogadorCommandHandler CriarHandler() => new(_jogadorRepository, _unitOfWork);

    [Fact]
    public async Task Handle_should_transfer_and_persist_when_jogador_exists_and_equipe_is_different()
    {
        var equipeOriginal = Guid.NewGuid();
        var novaEquipe = Guid.NewGuid();
        var jogador = Jogador.Create("jogador-1", equipeOriginal, "Bukayo Saka", "Atacante");
        _jogadorRepository.GetAsync(jogador.Id, Arg.Any<CancellationToken>()).Returns(jogador);

        Result resultado = await CriarHandler().Handle(new TransferirJogadorCommand(jogador.Id, novaEquipe), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        jogador.EquipeId.Should().Be(novaEquipe);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_return_failure_when_jogador_not_found()
    {
        var jogadorId = Guid.NewGuid();
        _jogadorRepository.GetAsync(jogadorId, Arg.Any<CancellationToken>()).Returns((Jogador?)null);

        Result resultado = await CriarHandler().Handle(new TransferirJogadorCommand(jogadorId, Guid.NewGuid()), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(JogadorErrors.NotFound(jogadorId));
    }

    [Fact]
    public async Task Handle_should_return_failure_when_nova_equipe_is_the_same_as_current()
    {
        var equipeId = Guid.NewGuid();
        var jogador = Jogador.Create("jogador-1", equipeId, "Bukayo Saka", "Atacante");
        _jogadorRepository.GetAsync(jogador.Id, Arg.Any<CancellationToken>()).Returns(jogador);

        Result resultado = await CriarHandler().Handle(new TransferirJogadorCommand(jogador.Id, equipeId), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(JogadorErrors.JaNaEquipe);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
