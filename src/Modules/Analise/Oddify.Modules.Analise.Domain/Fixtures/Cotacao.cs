using Oddify.Common.Domain;

namespace Oddify.Modules.Analise.Domain.Fixtures;

// Espelho local de Cotacao (módulo Fixtures), sincronizado via CotacaoColetadaIntegrationEvent —
// nunca criado/editado por um caso de uso deste módulo. Insert-only, mesma imutabilidade da
// entidade original: uma nova cotação coletada é sempre uma linha nova, nunca uma atualização.
// Sem eventos de domínio próprios (§8 caso 1).
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

    public static Cotacao Create(Guid id, Guid partidaId, string mercado, decimal odd, string casa, DateTime coletadaEmUtc)
    {
        var cotacao = new Cotacao
        {
            Id = id,
            PartidaId = partidaId,
            Mercado = mercado,
            Odd = odd,
            Casa = casa,
            ColetadaEmUtc = coletadaEmUtc
        };

        return cotacao;
    }
}
