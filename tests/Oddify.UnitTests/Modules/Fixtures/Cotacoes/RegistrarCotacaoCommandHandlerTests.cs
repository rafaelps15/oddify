using FluentAssertions;
using NSubstitute;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Application.Abstractions.Data;
using Oddify.Modules.Fixtures.Application.Cotacoes.RegistrarCotacao;
using Oddify.Modules.Fixtures.Domain.Cotacoes;
using Oddify.Modules.Fixtures.Domain.Partidas;

namespace Oddify.UnitTests.Modules.Fixtures.Cotacoes;

public sealed class RegistrarCotacaoCommandHandlerTests
{
    private readonly ICotacaoRepository _cotacaoRepository = Substitute.For<ICotacaoRepository>();
    private readonly IPartidaRepository _partidaRepository = Substitute.For<IPartidaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private RegistrarCotacaoCommandHandler CriarHandler() => new(_cotacaoRepository, _partidaRepository, _unitOfWork);

    [Fact]
    public async Task Handle_should_create_cotacao_and_persist_when_partida_exists_and_odd_is_valid()
    {
        var partida = Partida.Create("partida-1", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, rodada: 1, temporada: 2026);
        _partidaRepository.GetAsync(partida.Id, Arg.Any<CancellationToken>()).Returns(partida);

        var command = new RegistrarCotacaoCommand(partida.Id, "vitoria_casa", 1.5m, "casa-de-apostas", DateTime.UtcNow);

        Result<Guid> resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        _cotacaoRepository.Received(1).Insert(Arg.Is<Cotacao>(c => c.PartidaId == partida.Id && c.Odd == 1.5m));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_return_failure_when_partida_not_found()
    {
        var partidaId = Guid.NewGuid();
        _partidaRepository.GetAsync(partidaId, Arg.Any<CancellationToken>()).Returns((Partida?)null);

        var command = new RegistrarCotacaoCommand(partidaId, "vitoria_casa", 1.5m, "casa-de-apostas", DateTime.UtcNow);

        Result<Guid> resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(PartidaErrors.NotFound(partidaId));
        _cotacaoRepository.DidNotReceive().Insert(Arg.Any<Cotacao>());
    }

    [Fact]
    public async Task Handle_should_return_failure_when_odd_is_invalid()
    {
        var partida = Partida.Create("partida-1", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, rodada: 1, temporada: 2026);
        _partidaRepository.GetAsync(partida.Id, Arg.Any<CancellationToken>()).Returns(partida);

        var command = new RegistrarCotacaoCommand(partida.Id, "vitoria_casa", 0.9m, "casa-de-apostas", DateTime.UtcNow);

        Result<Guid> resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(CotacaoErrors.OddInvalida);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
