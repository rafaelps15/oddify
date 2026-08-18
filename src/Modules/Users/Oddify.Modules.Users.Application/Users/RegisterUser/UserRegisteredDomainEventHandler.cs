using Oddify.Common.Application.Messaging;
using Oddify.Modules.Users.Application.Abstractions.Data;
using Oddify.Modules.Users.Application.Users.EmailVerification;
using Oddify.Modules.Users.Domain.Users;

namespace Oddify.Modules.Users.Application.Users.RegisterUser;

// Despachado pelo OutboxProcessorJob (fora do request original de cadastro) — só gera+persiste o
// token e enfileira o e-mail de verificação na outbox (EmailVerificationTokenIssuer); o envio de
// fato acontece depois, via SendVerificationEmailIntegrationEventConsumer.
internal sealed class UserRegisteredDomainEventHandler(
    EmailVerificationTokenIssuer tokenIssuer,
    IUnitOfWork unitOfWork)
    : IDomainEventHandler<UserRegisteredDomainEvent>
{
    public async Task Handle(UserRegisteredDomainEvent notification, CancellationToken cancellationToken)
    {
        await tokenIssuer.IssueAsync(notification.UserId, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
