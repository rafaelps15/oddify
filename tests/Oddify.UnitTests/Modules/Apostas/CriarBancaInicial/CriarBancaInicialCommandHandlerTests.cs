using FluentAssertions;
using NSubstitute;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.Abstractions.Data;
using Oddify.Modules.Apostas.Application.Bancas.CriarBancaInicial;
using Oddify.Modules.Apostas.Domain.Bancas;

namespace Oddify.UnitTests.Modules.Apostas.CriarBancaInicial;

public sealed class CriarBancaInicialCommandHandlerTests
{
    private readonly IBancaRepository _bancaRepository = Substitute.For<IBancaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private CriarBancaInicialCommandHandler CriarHandler() => new(_bancaRepository, _unitOfWork);

    [Fact]
    public async Task Handle_should_create_banca_inicial_when_user_has_none()
    {
        var usuarioId = Guid.NewGuid();
        _bancaRepository.ExistsForUsuarioAsync(usuarioId, Arg.Any<CancellationToken>()).Returns(false);

        Result resultado = await CriarHandler().Handle(
            new CriarBancaInicialCommand(usuarioId, DateTime.UtcNow), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        _bancaRepository.Received(1).Insert(Arg.Is<Banca>(b => b.UsuarioId == usuarioId));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_be_idempotent_when_user_already_has_a_banca()
    {
        var usuarioId = Guid.NewGuid();
        _bancaRepository.ExistsForUsuarioAsync(usuarioId, Arg.Any<CancellationToken>()).Returns(true);

        Result resultado = await CriarHandler().Handle(
            new CriarBancaInicialCommand(usuarioId, DateTime.UtcNow), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        _bancaRepository.DidNotReceive().Insert(Arg.Any<Banca>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
