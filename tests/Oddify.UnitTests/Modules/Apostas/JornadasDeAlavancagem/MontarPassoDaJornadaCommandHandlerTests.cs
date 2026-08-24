using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Oddify.Common.Application.Authentication;
using Oddify.Common.Application.Clock;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.Abstractions.Data;
using Oddify.Modules.Apostas.Application.JornadasDeAlavancagem.GetOportunidadesParaAlavancagem;
using Oddify.Modules.Apostas.Application.JornadasDeAlavancagem.MontarPassoDaJornada;
using Oddify.Modules.Apostas.Domain.AnalisesDisponiveis;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;
using Oddify.Modules.Apostas.Domain.Bancas;
using Oddify.Modules.Apostas.Domain.JornadasDeAlavancagem;
using Oddify.Modules.Apostas.Domain.PassosDaJornada;
using Oddify.Modules.Apostas.Domain.PernasDeAposta;

namespace Oddify.UnitTests.Modules.Apostas.JornadasDeAlavancagem;

public sealed class MontarPassoDaJornadaCommandHandlerTests
{
    private readonly IJornadaDeAlavancagemRepository _jornadaDeAlavancagemRepository = Substitute.For<IJornadaDeAlavancagemRepository>();
    private readonly IBancaRepository _bancaRepository = Substitute.For<IBancaRepository>();
    private readonly IPassoDaJornadaRepository _passoDaJornadaRepository = Substitute.For<IPassoDaJornadaRepository>();
    private readonly IAnaliseDisponivelParaApostaRepository _analiseDisponivelRepository = Substitute.For<IAnaliseDisponivelParaApostaRepository>();
    private readonly IApostaMultiplaRepository _apostaMultiplaRepository = Substitute.For<IApostaMultiplaRepository>();
    private readonly IPernaDeApostaRepository _pernaDeApostaRepository = Substitute.For<IPernaDeApostaRepository>();
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly Guid _usuarioId = Guid.NewGuid();

    private MontarPassoDaJornadaCommandHandler CriarHandler()
    {
        _userContext.UserId.Returns(_usuarioId);
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);
        return new(
            _jornadaDeAlavancagemRepository, _bancaRepository, _passoDaJornadaRepository, _analiseDisponivelRepository,
            _apostaMultiplaRepository, _pernaDeApostaRepository, _sender, _unitOfWork, _userContext, _dateTimeProvider);
    }

    private Banca CriarBanca(decimal saldoInicial) =>
        Banca.Create(_usuarioId, "Banca principal", saldoInicial, 0.05m, PerfilDeRisco.Moderado, modoPaperTrading: true, FinalidadeDaBanca.Principal, DateTime.UtcNow);

    [Fact]
    public async Task Handle_should_return_conflito_de_concorrencia_when_save_throws_concurrency_exception()
    {
        Banca banca = CriarBanca(1000m);
        var jornada = JornadaDeAlavancagem.Create(
            _usuarioId, banca.Id, FaixaDeMeta.Dobrar, valorInicial: 1000m, valorObjetivo: 2000m,
            numeroDeFracoes: 1, totalDePassos: 4, probabilidadeDeConclusao: 0.78m, DateTime.UtcNow);

        _jornadaDeAlavancagemRepository.GetAsync(jornada.Id, _usuarioId, Arg.Any<CancellationToken>()).Returns(jornada);
        _bancaRepository.GetAsync(banca.Id, _usuarioId, Arg.Any<CancellationToken>()).Returns(banca);

        var disponivel =
            AnaliseDisponivelParaAposta.Create(Guid.NewGuid(), Guid.NewGuid(), "vitoria_casa", 1.45m, 0.72m, reduzida: false);
        var oportunidade = new OportunidadeParaAlavancagemResponse(disponivel.Id, disponivel.PartidaId, disponivel.Mercado, disponivel.OddDeMercado, disponivel.ProbabilidadeConfirmada, 0.05m);

        _sender.Send(Arg.Any<GetOportunidadesParaAlavancagemQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<OportunidadeParaAlavancagemResponse>>([oportunidade]));

        _analiseDisponivelRepository.GetAsync(disponivel.Id, Arg.Any<CancellationToken>()).Returns(disponivel);

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new DbUpdateConcurrencyException("conflito simulado"));

        Result<Guid> resultado = await CriarHandler().Handle(new MontarPassoDaJornadaCommand(jornada.Id), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(CommonErrors.ConflitoDeConcorrencia);
    }
}
