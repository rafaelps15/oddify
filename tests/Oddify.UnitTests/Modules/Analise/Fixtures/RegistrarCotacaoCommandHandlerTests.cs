using FluentAssertions;
using NSubstitute;
using Oddify.Common.Domain;
using Oddify.Modules.Analise.Application.Abstractions.Data;
using Oddify.Modules.Analise.Application.Fixtures.RegistrarCotacao;
using Oddify.Modules.Analise.Domain.Fixtures;

namespace Oddify.UnitTests.Modules.Analise.Fixtures;

public sealed class RegistrarCotacaoCommandHandlerTests
{
    private readonly ICotacaoRepository _cotacaoRepository = Substitute.For<ICotacaoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private RegistrarCotacaoCommandHandler CriarHandler() => new(_cotacaoRepository, _unitOfWork);

    [Fact]
    public async Task Handle_should_insert_when_cotacao_is_not_mirrored_yet()
    {
        var cotacaoId = Guid.NewGuid();
        var partidaId = Guid.NewGuid();
        _cotacaoRepository.GetMaisRecenteAsync(partidaId, "vitoria_casa", Arg.Any<CancellationToken>()).Returns((Cotacao?)null);

        var command = new RegistrarCotacaoCommand(cotacaoId, partidaId, "vitoria_casa", 1.85m, "bet365", DateTime.UtcNow);

        Result resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        _cotacaoRepository.Received(1).Insert(Arg.Is<Cotacao>(c =>
            c.Id == cotacaoId && c.PartidaId == partidaId && c.Mercado == "vitoria_casa" && c.Odd == 1.85m && c.Casa == "bet365"));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_be_idempotent_when_the_same_cotacao_was_already_mirrored()
    {
        var cotacaoId = Guid.NewGuid();
        var partidaId = Guid.NewGuid();
        var cotacaoExistente = Cotacao.Create(cotacaoId, partidaId, "vitoria_casa", 1.85m, "bet365", DateTime.UtcNow);
        _cotacaoRepository.GetMaisRecenteAsync(partidaId, "vitoria_casa", Arg.Any<CancellationToken>()).Returns(cotacaoExistente);

        var command = new RegistrarCotacaoCommand(cotacaoId, partidaId, "vitoria_casa", 1.85m, "bet365", DateTime.UtcNow);

        Result resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        _cotacaoRepository.DidNotReceive().Insert(Arg.Any<Cotacao>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
