using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Domain.Cotacoes;

public sealed class Cotacao : Entity
{
    private Cotacao()
    {
    }

    public Guid Id { get; private set; }

    public Guid PartidaId { get; private set; }

    public string Mercado { get; private set; }

    public decimal Odd { get; private set; }

    public string Casa { get; private set; }

    public DateTime ColetadaEmUtc { get; private set; }

    public static Result<Cotacao> Create(Guid partidaId, string mercado, decimal odd, string casa, DateTime coletadaEmUtc)
    {
        if (odd <= 1.0m)
        {
            return Result.Failure<Cotacao>(CotacaoErrors.OddInvalida);
        }

        var cotacao = new Cotacao
        {
            Id = Guid.NewGuid(),
            PartidaId = partidaId,
            Mercado = mercado,
            Odd = odd,
            Casa = casa,
            ColetadaEmUtc = coletadaEmUtc
        };

        cotacao.Raise(new CotacaoColetadaDomainEvent(cotacao.Id));

        return cotacao;
    }
}
