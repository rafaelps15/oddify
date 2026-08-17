using Oddify.Common.Application.Authentication;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Users.Application.Abstractions.Data;
using Oddify.Modules.Users.Domain.Users;

namespace Oddify.Modules.Users.Application.Users.RevokeSession;

internal sealed class RevokeSessionCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork,
    IUserContext userContext)
    : ICommandHandler<RevokeSessionCommand>
{
    public async Task<Result> Handle(RevokeSessionCommand request, CancellationToken cancellationToken)
    {
        RefreshToken? session = await refreshTokenRepository.GetAsync(request.SessionId, cancellationToken);

        // "Não existe" e "existe mas não é sua" caem no mesmo erro de propósito — não dá pra
        // revogar sessão de outro usuário, e a resposta não deve deixar isso perceptível.
        if (session is null || session.UserId != userContext.UserId)
        {
            return Result.Failure(UserErrors.SessionNotFound);
        }

        refreshTokenRepository.Delete(session);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
