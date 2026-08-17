using Oddify.Common.Domain;

namespace Oddify.Modules.Users.Domain.Users;

public static class UserErrors
{
    public static Error NotFound(Guid userId) =>
        Error.NotFound("Users.NotFound", $"O usuário com o identificador {userId} não foi encontrado");

    public static readonly Error EmailAlreadyRegistered = Error.Conflict(
        "Users.EmailAlreadyRegistered",
        "Já existe um usuário cadastrado com este e-mail");

    public static readonly Error InvalidCredentials = Error.Problem(
        "Users.InvalidCredentials",
        "O e-mail ou a senha informados estão incorretos");

    public static readonly Error InvalidRefreshToken = Error.Problem(
        "Users.InvalidRefreshToken",
        "O refresh token informado é inválido ou está expirado");

    public static readonly Error EmailAlreadyVerified = Error.Problem(
        "Users.EmailAlreadyVerified",
        "O e-mail desta conta já foi verificado");

    public static readonly Error EmailNotVerified = Error.Problem(
        "Users.EmailNotVerified",
        "O e-mail desta conta ainda não foi verificado");

    public static readonly Error SessionNotFound = Error.NotFound(
        "Users.SessionNotFound",
        "A sessão informada não foi encontrada");
}
