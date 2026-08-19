using Oddify.Common.Domain;

namespace Oddify.Modules.Apostas.Domain.AnalisesDisponiveis;

public sealed class AnaliseDisponivelParaAposta : Entity
{
    private AnaliseDisponivelParaAposta()
    {
    }

    public Guid Id { get; private set; }

    public Guid PartidaId { get; private set; }

    public string Mercado { get; private set; }

    public decimal OddDeMercado { get; private set; }

    public decimal ProbabilidadeConfirmada { get; private set; }

    public bool Reduzida { get; private set; }

    public bool JaUtilizada { get; private set; }

    public static AnaliseDisponivelParaAposta Create(
        Guid analiseId, Guid partidaId, string mercado, decimal oddDeMercado,
        decimal probabilidadeConfirmada, bool reduzida)
    {
        var analiseDisponivelParaAposta = new AnaliseDisponivelParaAposta
        {
            Id = analiseId,
            PartidaId = partidaId,
            Mercado = mercado,
            OddDeMercado = oddDeMercado,
            ProbabilidadeConfirmada = probabilidadeConfirmada,
            Reduzida = reduzida,
            JaUtilizada = false
        };

        return analiseDisponivelParaAposta;
    }

    public Result MarcarComoUtilizada()
    {
        if (JaUtilizada)
        {
            return Result.Failure(AnaliseDisponivelParaApostaErrors.JaUtilizada(Id));
        }

        JaUtilizada = true;
        return Result.Success();
    }

    // Contraparte de MarcarComoUtilizada — chamada quando a ApostaMultipla que consumiu essa
    // análise é excluída, pra liberar a análise de volta pra "Montar múltipla".
    public Result LiberarUso()
    {
        if (!JaUtilizada)
        {
            return Result.Failure(AnaliseDisponivelParaApostaErrors.AindaNaoUtilizada(Id));
        }

        JaUtilizada = false;
        return Result.Success();
    }
}
