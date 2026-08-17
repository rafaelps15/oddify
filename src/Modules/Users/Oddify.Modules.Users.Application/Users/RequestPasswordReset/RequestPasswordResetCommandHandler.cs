using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Users.Application.Abstractions.Data;
using Oddify.Modules.Users.Application.Users.PasswordReset;
using Oddify.Modules.Users.Domain.Users;

namespace Oddify.Modules.Users.Application.Users.RequestPasswordReset;

internal sealed class RequestPasswordResetCommandHandler(
    IUserRepository userRepository,
    PasswordResetTokenIssuer tokenIssuer,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RequestPasswordResetCommand>
{
    public async Task<Result> Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken)
    {
        User? user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);

        // Sempre sucesso, mesmo quando o e-mail não existe — não revelar quais e-mails têm conta
        // é o comportamento esperado pelo front (ver ForgotPasswordStore.requestReset) e a prática
        // padrão de mercado pra esse endpoint. Sem usuário, não há nada mais a fazer.
        if (user is null)
        {
            return Result.Success();
        }

        await tokenIssuer.IssueAsync(user.Id, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
