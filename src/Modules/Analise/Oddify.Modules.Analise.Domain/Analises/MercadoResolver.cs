namespace Oddify.Modules.Analise.Domain.Analises;

public static class MercadoResolver
{
    public static bool Resolver(string mercado, int golsCasa, int golsVisitante)
    {
        if (TentarExtrairLinha(mercado, "over_", out decimal linhaOver))
        {
            return golsCasa + golsVisitante > linhaOver;
        }

        if (TentarExtrairLinha(mercado, "under_", out decimal linhaUnder))
        {
            return golsCasa + golsVisitante < linhaUnder;
        }

        return mercado switch
        {
            "vitoria_casa" => golsCasa > golsVisitante,
            "empate" => golsCasa == golsVisitante,
            "vitoria_visitante" => golsVisitante > golsCasa,
            "ambos_marcam" => golsCasa > 0 && golsVisitante > 0,
            "ambos_marcam_nao" => golsCasa == 0 || golsVisitante == 0,
            _ => throw new ArgumentException($"Mercado desconhecido: {mercado}", nameof(mercado))
        };
    }

    private static bool TentarExtrairLinha(string mercado, string prefixo, out decimal linha)
    {
        linha = 0;
        if (!mercado.StartsWith(prefixo, StringComparison.Ordinal))
        {
            return false;
        }

        string parte = mercado[prefixo.Length..].Replace('_', '.');
        return decimal.TryParse(parte, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out linha);
    }
}
