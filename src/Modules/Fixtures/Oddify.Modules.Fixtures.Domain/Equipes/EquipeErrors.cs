using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Domain.Equipes;

public static class EquipeErrors
{
    public static Error NotFound(Guid equipeId) =>
        Error.NotFound("Equipes.NotFound", $"A equipe com o identificador {equipeId} não foi encontrada");
}
