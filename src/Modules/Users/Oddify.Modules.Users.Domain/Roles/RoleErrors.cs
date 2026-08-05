using Oddify.Common.Domain;

namespace Oddify.Modules.Users.Domain.Roles;

public static class RoleErrors
{
    public static Error NotFound(Guid roleId) =>
        Error.NotFound("Roles.NotFound", $"O papel com o identificador {roleId} não foi encontrado");

    public static Error NotFoundByName(string name) =>
        Error.NotFound("Roles.NotFoundByName", $"O papel com o nome {name} não foi encontrado");

    public static readonly Error AlreadyAssigned = Error.Conflict(
        "Roles.AlreadyAssigned",
        "O usuário já possui este papel atribuído");

    public static readonly Error NotAssigned = Error.Problem(
        "Roles.NotAssigned",
        "O usuário não possui este papel atribuído");
}
