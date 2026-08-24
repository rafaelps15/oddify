namespace Oddify.Modules.Analise.Domain.Analises;

public static class MercadoResolver
{
    // Fonte única dos mercados literais (não-linha) — Resolver, ObterGrupoDeMercados e EhConhecido
    // derivam todos daqui para nunca ficarem dessincronizados entre si.
    private static readonly HashSet<string> MercadosLiterais =
    [
        "vitoria_casa", "empate", "vitoria_visitante", "ambos_marcam", "ambos_marcam_nao"
    ];

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

        if (!MercadosLiterais.Contains(mercado))
        {
            throw new ArgumentException($"Mercado desconhecido: {mercado}", nameof(mercado));
        }

        return mercado switch
        {
            "vitoria_casa" => golsCasa > golsVisitante,
            "empate" => golsCasa == golsVisitante,
            "vitoria_visitante" => golsVisitante > golsCasa,
            "ambos_marcam" => golsCasa > 0 && golsVisitante > 0,
            _ => golsCasa == 0 || golsVisitante == 0 // ambos_marcam_nao
        };
    }

    // Valida o formato sem lançar — usado pelos Validators (RegistrarCotacaoCommandValidator,
    // AnalisarPartidaCommandValidator) para rejeitar mercado desconhecido antes do Handle rodar,
    // evitando que Resolver/ObterGrupoDeMercados lancem ArgumentException em produção (um mercado
    // malformado vindo da fonte externa de odds já travou o processamento do inbox por conta disso).
    public static bool EhConhecido(string mercado)
    {
        if (string.IsNullOrWhiteSpace(mercado))
        {
            return false;
        }

        if (TentarExtrairLinha(mercado, "over_", out _) || TentarExtrairLinha(mercado, "under_", out _))
        {
            return true;
        }

        return MercadosLiterais.Contains(mercado);
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

    /// <summary>
    /// Devolve os codigos de mercado que, juntos, particionam o mesmo espaco de eventos do mercado informado
    /// (ex.: "vitoria_casa" -> as tres saidas do 1X2; "over_2_5" -> over/under da mesma linha).
    /// Usado pelo RemovedorDeMargem para normalizar as probabilidades implicitas de um mercado inteiro.
    /// </summary>
    public static IReadOnlyCollection<string> ObterGrupoDeMercados(string mercado)
    {
        if (TentarExtrairSufixo(mercado, "over_", out string sufixoOver))
        {
            return [mercado, $"under_{sufixoOver}"];
        }

        if (TentarExtrairSufixo(mercado, "under_", out string sufixoUnder))
        {
            return [$"over_{sufixoUnder}", mercado];
        }

        if (!MercadosLiterais.Contains(mercado))
        {
            throw new ArgumentException($"Mercado desconhecido: {mercado}", nameof(mercado));
        }

        return mercado switch
        {
            "vitoria_casa" or "empate" or "vitoria_visitante" => ["vitoria_casa", "empate", "vitoria_visitante"],
            _ => ["ambos_marcam", "ambos_marcam_nao"] // "ambos_marcam" or "ambos_marcam_nao"
        };
    }

    private static bool TentarExtrairSufixo(string mercado, string prefixo, out string sufixo)
    {
        sufixo = string.Empty;
        if (!mercado.StartsWith(prefixo, StringComparison.Ordinal))
        {
            return false;
        }

        sufixo = mercado[prefixo.Length..];
        return true;
    }
}
