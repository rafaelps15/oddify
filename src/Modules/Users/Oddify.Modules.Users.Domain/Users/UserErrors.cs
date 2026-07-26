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
}
