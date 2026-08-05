using MassTransit;
using NSubstitute;
using Oddify.Modules.Apostas.Application.Abstractions.Data;
using Oddify.Modules.Apostas.Domain.Bancas;
using Oddify.Modules.Apostas.Presentation.IntegrationEvents;
using Oddify.Modules.Users.IntegrationEvents;

namespace Oddify.UnitTests.Modules.Apostas.IntegrationEvents;

public sealed class EmailVerifiedIntegrationEventConsumerTests
{
    private readonly IBancaRepository _bancaRepository = Substitute.For<IBancaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private EmailVerifiedIntegrationEventConsumer CriarConsumer() => new(_bancaRepository, _unitOfWork);

    private static ConsumeContext<EmailVerifiedIntegrationEvent> CriarContexto(EmailVerifiedIntegrationEvent evento)
    {
        ConsumeContext<EmailVerifiedIntegrationEvent> context = Substitute.For<ConsumeContext<EmailVerifiedIntegrationEvent>>();
        context.Message.Returns(evento);
        context.CancellationToken.Returns(CancellationToken.None);
        return context;
    }

    [Fact]
    public async Task Consume_should_create_banca_inicial_when_user_has_none()
    {
        var evento = new EmailVerifiedIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid());
        _bancaRepository.ExistsForUsuarioAsync(evento.UserId, Arg.Any<CancellationToken>()).Returns(false);

        await CriarConsumer().Consume(CriarContexto(evento));

        _bancaRepository.Received(1).Insert(Arg.Is<Banca>(b => b.UsuarioId == evento.UserId));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_should_be_idempotent_when_user_already_has_a_banca()
    {
        var evento = new EmailVerifiedIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid());
        _bancaRepository.ExistsForUsuarioAsync(evento.UserId, Arg.Any<CancellationToken>()).Returns(true);

        await CriarConsumer().Consume(CriarContexto(evento));

        _bancaRepository.DidNotReceive().Insert(Arg.Any<Banca>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
