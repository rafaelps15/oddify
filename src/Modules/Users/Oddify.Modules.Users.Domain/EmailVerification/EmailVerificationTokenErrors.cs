using Oddify.Common.Domain;

namespace Oddify.Modules.Users.Domain.EmailVerification;

public static class EmailVerificationTokenErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "EmailVerificationTokens.NotFound",
        "O token de verificação não foi encontrado");

    public static readonly Error AlreadyConsumed = Error.Problem(
        "EmailVerificationTokens.AlreadyConsumed",
        "Este token de verificação já foi utilizado");

    public static readonly Error Expired = Error.Problem(
        "EmailVerificationTokens.Expired",
        "Este token de verificação expirou");
}
