using FluentAssertions;
using NSubstitute;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.Abstractions.Data;
using Oddify.Modules.Apostas.Application.ApostasMultiplas.RegistrarAnaliseDisponivelParaAposta;
using Oddify.Modules.Apostas.Domain.AnalisesDisponiveis;

namespace Oddify.UnitTests.Modules.Apostas.RegistrarAnaliseDisponivelParaAposta;

public sealed class RegistrarAnaliseDisponivelParaApostaCommandHandlerTests
{
    private readonly IAnaliseDisponivelParaApostaRepository _repository = Substitute.For<IAnaliseDisponivelParaApostaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private RegistrarAnaliseDisponivelParaApostaCommandHandler CriarHandler() => new(_repository, _unitOfWork);

    [Fact]
    public async Task Handle_should_insert_analise_disponivel_and_persist()
    {
        var command = new RegistrarAnaliseDisponivelParaApostaCommand(
            Guid.NewGuid(), Guid.NewGuid(), "vitoria_casa", 1.85m, 0.62m, Reduzida: false);

        Result resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        _repository.Received(1).Insert(Arg.Is<AnaliseDisponivelParaAposta>(a =>
            a.Id == command.AnaliseId &&
            a.PartidaId == command.PartidaId &&
            a.Mercado == command.Mercado &&
            a.OddDeMercado == command.OddDeMercado &&
            a.ProbabilidadeConfirmada == command.ProbabilidadeConfirmada &&
            a.Reduzida == command.Reduzida));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
