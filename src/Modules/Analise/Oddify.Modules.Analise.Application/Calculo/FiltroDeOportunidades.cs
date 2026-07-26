namespace Oddify.Modules.Analise.Application.Calculo;

internal static class FiltroDeOportunidades
{
    public const decimal VantagemMinima = 0.04m;
    public const decimal OddMinima = 1.40m;
    public const decimal OddMaxima = 1.70m;
    public const int AmostraMinima = 8;

    public static (bool Aprovada, string? MotivoDoDescarte) Avaliar(decimal vantagem, decimal odd, int amostraDeJogos, bool ligaCalibrada)
    {
        var motivos = new List<string>();

        if (vantagem < VantagemMinima)
        {
            motivos.Add($"Vantagem {vantagem:P2} abaixo do mínimo de {VantagemMinima:P2}");
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
