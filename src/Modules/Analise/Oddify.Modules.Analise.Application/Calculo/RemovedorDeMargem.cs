using Oddify.Modules.Analise.Domain.Analises;

namespace Oddify.Modules.Analise.Application.Calculo;

/// <summary>
/// Remove a margem da casa de apostas de um mercado, normalizando as probabilidades implicitas
/// brutas (1/odd) de todas as saidas do mercado para que somem 100%. Etapa obrigatoria antes de
/// calcular qualquer edge: comparar o modelo contra 1/odd bruto infla o edge artificialmente,
/// porque a soma das implicitas brutas de um mercado real e sempre maior que 100% (a margem).
/// </summary>
internal static class RemovedorDeMargem
{
    public static decimal Remover(string mercado, IReadOnlyDictionary<string, decimal> oddsPorMercado)
    {
        IReadOnlyCollection<string> grupo = MercadoResolver.ObterGrupoDeMercados(mercado);

        decimal somaDeImplicitasBrutas = 0m;
        foreach (string mercadoDoGrupo in grupo)
        {
            if (!oddsPorMercado.TryGetValue(mercadoDoGrupo, out decimal odd))
            {
                throw new InvalidOperationException(
                    $"Odd do mercado '{mercadoDoGrupo}' ausente — necessária para remover a margem do grupo de '{mercado}'.");
            }

            somaDeImplicitasBrutas += 1m / odd;
        }

        decimal implicitaBrutaDoMercado = 1m / oddsPorMercado[mercado];

        return implicitaBrutaDoMercado / somaDeImplicitasBrutas;
    }

    /// <summary>Confere se todas as odds do grupo do mercado estao disponiveis antes de tentar remover a margem.</summary>
    public static bool GrupoCompleto(string mercado, IReadOnlyDictionary<string, decimal> oddsPorMercado)
    {
        return MercadoResolver.ObterGrupoDeMercados(mercado).All(oddsPorMercado.ContainsKey);
    }
}
