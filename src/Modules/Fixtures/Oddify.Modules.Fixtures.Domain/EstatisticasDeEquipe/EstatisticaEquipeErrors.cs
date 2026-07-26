using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Domain.EstatisticasDeEquipe;

public static class EstatisticaEquipeErrors
{
    public static Error NotFound(Guid estatisticaEquipeId) =>
        Error.NotFound(
            "EstatisticasDeEquipe.NotFound",
            $"A estatística de equipe com o identificador {estatisticaEquipeId} não foi encontrada");
}
