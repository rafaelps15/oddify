using Oddify.Common.Domain;

namespace Oddify.Modules.Users.Domain.PasswordReset;

public static class PasswordResetTokenErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "PasswordResetTokens.NotFound",
        "O token de redefinição de senha não foi encontrado");

    public static readonly Error AlreadyConsumed = Error.Problem(
        "PasswordResetTokens.AlreadyConsumed",
        "Este token de redefinição de senha já foi utilizado");

    public static readonly Error Expired = Error.Problem(
        "PasswordResetTokens.Expired",
        "Este token de redefinição de senha expirou");
}
