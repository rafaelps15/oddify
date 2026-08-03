namespace Oddify.Modules.Analise.Application.Calculo;

internal static class FiltroDeOportunidades
{
    /// <summary>Piso absoluto de edge (9.1.0 da spec): nenhum limiar por estratégia pode ficar abaixo disso.</summary>
    public const decimal EdgeMinimoAbsoluto = 0.03m;

    /// <summary>Limiar padrão do filtro geral (4 p.p.), usado quando a estratégia não define o próprio limiar.</summary>
    public const decimal VantagemMinima = 0.04m;

    public const decimal OddMinima = 1.40m;
    public const decimal OddMaxima = 1.70m;
    public const int AmostraMinima = 8;

    public static (bool Aprovada, string? MotivoDoDescarte) Avaliar(
        decimal vantagem,
        decimal odd,
        int amostraDeJogos,
        bool ligaCalibrada,
        decimal? vantagemMinima = null)
    {
        decimal limiarEfetivo = Math.Max(vantagemMinima ?? VantagemMinima, EdgeMinimoAbsoluto);

        var motivos = new List<string>();

        if (vantagem < limiarEfetivo)
        {
            motivos.Add($"Vantagem {vantagem:P2} abaixo do mínimo de {limiarEfetivo:P2}");
        }

        if (odd < OddMinima || odd > OddMaxima)
        {
            motivos.Add($"Odd {odd} fora da faixa [{OddMinima}, {OddMaxima}]");
        }

        if (amostraDeJogos < AmostraMinima)
        {
            motivos.Add($"Amostra de {amostraDeJogos} jogos abaixo do mínimo de {AmostraMinima}");
        }

        if (!ligaCalibrada)
        {
            motivos.Add("Liga não está calibrada");
        }

        return motivos.Count == 0 ? (true, null) : (false, string.Join("; ", motivos));
    }
}
