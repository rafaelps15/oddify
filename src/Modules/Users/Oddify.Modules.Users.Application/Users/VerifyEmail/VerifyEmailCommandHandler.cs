using Oddify.Common.Application.Clock;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Users.Application.Abstractions.Data;
using Oddify.Modules.Users.Domain.EmailVerification;
using Oddify.Modules.Users.Domain.Users;

namespace Oddify.Modules.Users.Application.Users.VerifyEmail;

internal sealed class VerifyEmailCommandHandler(
    IEmailVerificationTokenRepository tokenRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<VerifyEmailCommand>
{
    public async Task<Result> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        string tokenHash = EmailVerificationToken.Hash(request.Token);

        EmailVerificationToken? token = await tokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (token is null)
        {
            return Result.Failure(EmailVerificationTokenErrors.NotFound);
        }

        DateTime agora = dateTimeProvider.UtcNow;

        if (token.ConsumedAtUtc is not null)
        {
            return Result.Failure(EmailVerificationTokenErrors.AlreadyConsumed);
        }

        if (token.ExpiresAtUtc < agora)
        {
            return Result.Failure(EmailVerificationTokenErrors.Expired);
        }

        User? user = await userRepository.GetAsync(token.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound(token.UserId));
        }

        Result markResult = user.MarkEmailAsVerified(agora);
        if (markResult.IsFailure)
        {
            return markResult;
        }

        token.Consume(agora);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
