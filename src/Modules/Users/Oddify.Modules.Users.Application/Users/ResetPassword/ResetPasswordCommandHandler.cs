using Oddify.Common.Application.Authentication;
using Oddify.Common.Application.Clock;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Users.Application.Abstractions.Data;
using Oddify.Modules.Users.Domain.PasswordReset;
using Oddify.Modules.Users.Domain.Users;

namespace Oddify.Modules.Users.Application.Users.ResetPassword;

internal sealed class ResetPasswordCommandHandler(
    IPasswordResetTokenRepository tokenRepository,
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ResetPasswordCommand>
{
    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        string tokenHash = PasswordResetToken.Hash(request.Token);

        PasswordResetToken? token = await tokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (token is null)
        {
            return Result.Failure(PasswordResetTokenErrors.NotFound);
        }

        DateTime agora = dateTimeProvider.UtcNow;

        if (token.ConsumedAtUtc is not null)
        {
            return Result.Failure(PasswordResetTokenErrors.AlreadyConsumed);
        }

        if (token.ExpiresAtUtc < agora)
        {
            return Result.Failure(PasswordResetTokenErrors.Expired);
        }

        User? user = await userRepository.GetAsync(token.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound(token.UserId));
        }

        user.SetPassword(passwordHasher.Hash(request.NewPassword));
        token.Consume(agora);

        // Redefinir a senha invalida qualquer sessão aberta com credenciais antigas — força
        // relogin em cada dispositivo conectado, mesmo comportamento esperado de um fluxo de reset.
        IReadOnlyCollection<RefreshToken> sessoesAtivas = await refreshTokenRepository.GetByUserIdAsync(user.Id, cancellationToken);

        foreach (RefreshToken sessao in sessoesAtivas)
        {
            refreshTokenRepository.Delete(sessao);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
