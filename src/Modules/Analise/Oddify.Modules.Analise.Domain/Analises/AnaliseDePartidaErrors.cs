using Oddify.Common.Domain;

namespace Oddify.Modules.Analise.Domain.Analises;

public static class AnaliseDePartidaErrors
{
    public static Error NotFound(Guid analiseId) =>
        Error.NotFound("Analises.NotFound", $"A análise com o identificador {analiseId} não foi encontrada");

    public static Error NaoAprovadaNoFiltro(Guid analiseId) =>
        Error.Problem("Analises.NaoAprovadaNoFiltro", $"A análise com o identificador {analiseId} não foi aprovada no filtro de oportunidades");
}
