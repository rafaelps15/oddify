using Oddify.Common.Domain;

namespace Oddify.Modules.Users.Domain.Users;

public static class UserErrors
{
    public static Error NotFound(Guid userId) =>
        Error.NotFound("Users.NotFound", $"The user with the identifier {userId} not found");

    public static readonly Error EmailAlreadyRegistered = Error.Conflict(
        "Users.EmailAlreadyRegistered",
        "A user with this email is already registered");

    public static readonly Error InvalidCredentials = Error.Problem(
        "Users.InvalidCredentials",
        "The provided email or password is incorrect");

    public static readonly Error InvalidRefreshToken = Error.Problem(
        "Users.InvalidRefreshToken",
        "The provided refresh token is invalid or has expired");

    public static readonly Error EmailAlreadyVerified = Error.Problem(
        "Users.EmailAlreadyVerified",
        "This account's email is already verified");

    public static readonly Error EmailNotVerified = Error.Problem(
        "Users.EmailNotVerified",
        "This account's email has not been verified yet");
}
