using Oddify.Common.Domain;

namespace Oddify.Modules.Apostas.Domain.AnalisesDisponiveis;

public static class AnaliseDisponivelParaApostaErrors
{
    public static Error NotFound(Guid analiseId) =>
        Error.NotFound("AnalisesDisponiveis.NotFound", $"A análise disponível com o identificador {analiseId} não foi encontrada");

    public static Error JaUtilizada(Guid analiseId) =>
        Error.Problem("AnalisesDisponiveis.JaUtilizada", $"A análise com o identificador {analiseId} já foi utilizada em outra múltipla");
}
