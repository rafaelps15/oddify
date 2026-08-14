using FluentAssertions;
using NSubstitute;
using Oddify.Common.Application.Authentication;
using Oddify.Common.Application.Clock;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.Abstractions.Data;
using Oddify.Modules.Apostas.Application.ApostasMultiplas.AnularApostaMultipla;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;
using Oddify.Modules.Apostas.Domain.PernasDeAposta;

namespace Oddify.UnitTests.Modules.Apostas.AnularApostaMultipla;

public sealed class AnularApostaMultiplaCommandHandlerTests
{
    private readonly IApostaMultiplaRepository _apostaMultiplaRepository = Substitute.For<IApostaMultiplaRepository>();
    private readonly IPernaDeApostaRepository _pernaDeApostaRepository = Substitute.For<IPernaDeApostaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly Guid _usuarioId = Guid.NewGuid();

    private AnularApostaMultiplaCommandHandler CriarHandler()
    {
        _userContext.UserId.Returns(_usuarioId);
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);
        return new(_apostaMultiplaRepository, _pernaDeApostaRepository, _unitOfWork, _dateTimeProvider, _userContext);
    }

    [Fact]
    public async Task Handle_should_anular_aposta_and_pernas_when_pendente()
    {
        var apostaMultipla = ApostaMultipla.Create(
            _usuarioId, Guid.NewGuid(), oddCombinada: 4.0m, stake: 50m, OrigemDaAposta.ManualEntry, descricao: null, passoDaJornadaId: null, DateTime.UtcNow);
        var perna = PernaDeAposta.Create(apostaMultipla.Id, Guid.NewGuid(), Guid.NewGuid(), "vitoria_casa", 4.0m);

        _apostaMultiplaRepository.GetAsync(apostaMultipla.Id, _usuarioId, Arg.Any<CancellationToken>()).Returns(apostaMultipla);
        _pernaDeApostaRepository.GetPorApostaMultiplaAsync(apostaMultipla.Id, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<PernaDeAposta>)[perna]);

        Result resultado = await CriarHandler().Handle(new AnularApostaMultiplaCommand(apostaMultipla.Id), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        apostaMultipla.Resultado.Should().Be(ResultadoDaAposta.Anulada);
        apostaMultipla.LucroOuPerda.Should().Be(0m);
        perna.Resultado.Should().Be(ResultadoDaAposta.Anulada);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_fail_when_already_liquidada()
    {
        var apostaMultipla = ApostaMultipla.Create(
            _usuarioId, Guid.NewGuid(), oddCombinada: 4.0m, stake: 50m, OrigemDaAposta.ManualEntry, descricao: null, passoDaJornadaId: null, DateTime.UtcNow);
        apostaMultipla.Liquidar(true, DateTime.UtcNow);

        _apostaMultiplaRepository.GetAsync(apostaMultipla.Id, _usuarioId, Arg.Any<CancellationToken>()).Returns(apostaMultipla);

        Result resultado = await CriarHandler().Handle(new AnularApostaMultiplaCommand(apostaMultipla.Id), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(ApostaMultiplaErrors.JaDecidida(apostaMultipla.Id));
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_fail_when_aposta_not_found()
    {
        var apostaMultiplaId = Guid.NewGuid();
        _apostaMultiplaRepository.GetAsync(apostaMultiplaId, _usuarioId, Arg.Any<CancellationToken>()).Returns((ApostaMultipla?)null);

        Result resultado = await CriarHandler().Handle(new AnularApostaMultiplaCommand(apostaMultiplaId), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(ApostaMultiplaErrors.NotFound(apostaMultiplaId));
    }
}
