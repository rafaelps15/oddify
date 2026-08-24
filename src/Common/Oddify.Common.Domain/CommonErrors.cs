namespace Oddify.Common.Domain;

// Erro que não pertence a nenhum módulo específico — usado quando .UseXminAsConcurrencyToken()
// detecta que o registro foi alterado por outra operação entre a leitura e o SaveChanges.
public static class CommonErrors
{
    public static readonly Error ConflitoDeConcorrencia = Error.Conflict(
        "Concorrencia.Conflito",
        "O registro foi alterado por outra operação enquanto esta era processada. Tente novamente.");
}
