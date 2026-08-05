using Oddify.Common.Application.Authentication;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Users.Application.Abstractions.Data;
using Oddify.Modules.Users.Application.Users.EmailVerification;
using Oddify.Modules.Users.Domain.Users;

namespace Oddify.Modules.Users.Application.Users.ResendVerification;

internal sealed class ResendVerificationCommandHandler(
    IUserRepository userRepository,
    EmailVerificationTokenIssuer tokenIssuer,
    IUnitOfWork unitOfWork,
    IUserContext userContext)
    : ICommandHandler<ResendVerificationCommand>
{
    public async Task<Result> Handle(ResendVerificationCommand request, CancellationToken cancellationToken)
    {
        User? user = await userRepository.GetAsync(userContext.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound(userContext.UserId));
        }

        if (user.IsEmailVerified)
        {
            return Result.Failure(UserErrors.EmailAlreadyVerified);
        }

        await tokenIssuer.IssueAsync(user.Id, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
