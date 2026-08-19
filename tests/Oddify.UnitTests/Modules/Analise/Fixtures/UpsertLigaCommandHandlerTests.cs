using FluentAssertions;
using NSubstitute;
using Oddify.Common.Domain;
using Oddify.Modules.Analise.Application.Abstractions.Data;
using Oddify.Modules.Analise.Application.Fixtures.UpsertLiga;
using Oddify.Modules.Analise.Domain.Fixtures;

namespace Oddify.UnitTests.Modules.Analise.Fixtures;

public sealed class UpsertLigaCommandHandlerTests
{
    private readonly ILigaRepository _ligaRepository = Substitute.For<ILigaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private UpsertLigaCommandHandler CriarHandler() => new(_ligaRepository, _unitOfWork);

    [Fact]
    public async Task Handle_should_insert_when_liga_does_not_exist_yet()
    {
        var ligaId = Guid.NewGuid();
        _ligaRepository.GetAsync(ligaId, Arg.Any<CancellationToken>()).Returns((Liga?)null);

        var command = new UpsertLigaCommand(ligaId, "Liga de Teste", 2.5m, 1.1m, Calibrada: true);

        Result resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        _ligaRepository.Received(1).Insert(Arg.Is<Liga>(l =>
            l.Id == ligaId && l.Nome == "Liga de Teste" && l.MediaDeGols == 2.5m && l.FatorCasa == 1.1m && l.Calibrada));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_update_when_liga_already_exists()
    {
        var ligaId = Guid.NewGuid();
        var liga = Liga.Create(ligaId, "Nome Antigo", 2.0m, 1.0m, calibrada: false);
        _ligaRepository.GetAsync(ligaId, Arg.Any<CancellationToken>()).Returns(liga);

        var command = new UpsertLigaCommand(ligaId, "Nome Novo", 2.8m, 1.2m, Calibrada: true);

        Result resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        liga.Nome.Should().Be("Nome Novo");
        liga.MediaDeGols.Should().Be(2.8m);
        liga.FatorCasa.Should().Be(1.2m);
        liga.Calibrada.Should().BeTrue();
        _ligaRepository.DidNotReceive().Insert(Arg.Any<Liga>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
