using Oddify.Common.Domain;

namespace Oddify.Modules.Users.Domain.EmailVerification;

public static class EmailVerificationTokenErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "EmailVerificationTokens.NotFound",
        "The verification token was not found");

    public static readonly Error AlreadyConsumed = Error.Problem(
        "EmailVerificationTokens.AlreadyConsumed",
        "This verification token has already been used");

    public static readonly Error Expired = Error.Problem(
        "EmailVerificationTokens.Expired",
        "This verification token has expired");
}
