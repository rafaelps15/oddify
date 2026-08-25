using Oddify.Modules.Apostas.Application.Bancas.GetDesempenhoPorMercado;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;

namespace Oddify.Modules.Apostas.Application.Calculo;

// Passo de agregação compartilhado por GetDesempenhoPorTimeQueryHandler e
// GetDesempenhoPorCampeonatoQueryHandler — os dois só diferem em como resolvem a chave (por time
// ou por campeonato, via ChaveDeDesempenho); agrupar e somar depois disso é idêntico nos dois, e
// vivia duplicado em cada handler antes desta extração.
internal static class DesempenhoCalculator
{
    public static List<DesempenhoResponse> Agrupar(IEnumerable<(string Chave, ApostaComPartidaRow Row)> entradas) =>
        entradas
            .GroupBy(e => e.Chave)
            .Select(g => new DesempenhoResponse(
                g.Key,
                g.Count(),
                g.Count(e => e.Row.Resultado is ResultadoDaAposta.Ganha or ResultadoDaAposta.MeioGanha),
                g.Count(e => e.Row.Resultado is ResultadoDaAposta.Perdida or ResultadoDaAposta.MeioPerdida),
                g.Sum(e => e.Row.LucroOuPerda),
                g.Sum(e => e.Row.Stake) > 0 ? g.Sum(e => e.Row.LucroOuPerda) / g.Sum(e => e.Row.Stake) : null))
            .OrderByDescending(d => d.Lucro)
            .ToList();
}
