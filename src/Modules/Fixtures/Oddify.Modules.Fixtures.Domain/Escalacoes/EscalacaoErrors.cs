using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Domain.Escalacoes;

public static class EscalacaoErrors
{
    public static Error NotFound(Guid escalacaoId) =>
        Error.NotFound("Escalacoes.NotFound", $"A escalação com o identificador {escalacaoId} não foi encontrada");
}
