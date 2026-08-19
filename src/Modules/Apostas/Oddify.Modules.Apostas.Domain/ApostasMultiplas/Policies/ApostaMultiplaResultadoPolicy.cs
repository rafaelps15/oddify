namespace Oddify.Modules.Apostas.Domain.ApostasMultiplas.Policies;

// Decisão pura que atravessa a aposta múltipla e as pernas resolvidas: só ganha se todas as pernas
// tiverem ganho. Classe estática, sem estado, sem I/O — mesmo papel de MeetingGroupExpirationDatePolicy
// no modular-monolith-with-ddd (Kamil Grzybek): cálculo que não pertence a uma única entidade fica
// numa Policy ao lado do agregado, não numa Service injetada.
public static class ApostaMultiplaResultadoPolicy
{
    public static bool Ganhou(IReadOnlyCollection<bool> resultadosDasPernas)
        => resultadosDasPernas.Count > 0 && resultadosDasPernas.All(ganhou => ganhou);
}
