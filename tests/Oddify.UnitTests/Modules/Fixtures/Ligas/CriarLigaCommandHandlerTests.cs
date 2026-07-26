using FluentAssertions;
using NSubstitute;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Application.Abstractions.Data;
using Oddify.Modules.Fixtures.Application.Ligas.CriarLiga;
using Oddify.Modules.Fixtures.Domain.Ligas;

namespace Oddify.UnitTests.Modules.Fixtures.Ligas;

public sealed class CriarLigaCommandHandlerTests
{
    private readonly ILigaConfiguradaRepository _ligaRepository = Substitute.For<ILigaConfiguradaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private CriarLigaCommandHandler CriarHandler() => new(_ligaRepository, _unitOfWork);

    [Fact]
    public async Task Handle_should_create_liga_and_persist_when_idExterno_is_not_registered()
    {
        _ligaRepository.GetByIdExternoAsync("liga-1", Arg.Any<CancellationToken>()).Returns((LigaConfigurada?)null);

        var command = new CriarLigaCommand("liga-1", "Premier League", 2.5m, 1.1m);

        Result<Guid> resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        _ligaRepository.Received(1).Insert(Arg.Is<LigaConfigurada>(l => l.IdExterno == "liga-1" && l.Nome == "Premier League"));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_fail_when_idExterno_is_already_registered()
    {
        var ligaExistente = LigaConfigurada.Create("liga-1", "Premier League", 2.5m, 1.1m);
        _ligaRepository.GetByIdExternoAsync("liga-1", Arg.Any<CancellationToken>()).Returns(ligaExistente);

        var command = new CriarLigaCommand("liga-1", "Outra Liga", 2.0m, 1.0m);

        Result<Guid> resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(LigaConfiguradaErrors.IdExternoJaCadastrado);
        _ligaRepository.DidNotReceive().Insert(Arg.Any<LigaConfigurada>());
    }
}
