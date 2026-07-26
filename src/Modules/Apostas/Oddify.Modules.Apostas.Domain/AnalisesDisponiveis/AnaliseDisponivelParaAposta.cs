using Oddify.Common.Domain;

namespace Oddify.Modules.Apostas.Domain.AnalisesDisponiveis;

public sealed class AnaliseDisponivelParaAposta : Entity
{
    private AnaliseDisponivelParaAposta(
        Guid id, Guid partidaId, string mercado, decimal oddDeMercado,
        decimal probabilidadeConfirmada, bool reduzida)
    {
        Id = id;
        PartidaId = partidaId;
        Mercado = mercado;
        OddDeMercado = oddDeMercado;
        ProbabilidadeConfirmada = probabilidadeConfirmada;
        Reduzida = reduzida;
        JaUtilizada = false;
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
        decimal probabilidadeConfirmada, bool reduzida) =>
        new(analiseId, partidaId, mercado, oddDeMercado, probabilidadeConfirmada, reduzida);

    public Result MarcarComoUtilizada()
    {
        if (JaUtilizada)
        {
            return Result.Failure(AnaliseDisponivelParaApostaErrors.JaUtilizada(Id));
        }

        JaUtilizada = true;
        return Result.Success();
    }
}
