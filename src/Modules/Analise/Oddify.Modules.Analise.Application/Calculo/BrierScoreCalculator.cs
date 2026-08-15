using Oddify.Modules.Analise.Domain.Analises;

namespace Oddify.Modules.Analise.Application.Calculo;

// Brier score: erro quadrático médio entre a probabilidade prevista e o resultado real (0 ou 1).
// Quanto menor, melhor a calibração do modelo. BrierPosClaude mede só as análises em que o
// Claude confirmou ou reduziu a aprovação do filtro (nunca as que ele rejeitou).
public static class BrierScoreCalculator
{
    public static ResultadoDaMedicao Calcular(IReadOnlyCollection<AmostraDeMedicao> amostras)
    {
        decimal somaErroPoissonPuro = 0m;
        decimal somaErroDixonColes = 0m;
        int amostraTotal = 0;

        decimal somaErroPosClaude = 0m;
        int amostraPosClaude = 0;

        foreach (AmostraDeMedicao amostra in amostras)
        {
            decimal erroPoissonPuro = amostra.ProbPoissonPura - amostra.ResultadoReal;
            decimal erroDixonColes = amostra.ProbDixonColes - amostra.ResultadoReal;

            somaErroPoissonPuro += erroPoissonPuro * erroPoissonPuro;
            somaErroDixonColes += erroDixonColes * erroDixonColes;
            amostraTotal++;

            if (amostra.DecisaoDoClaude == DecisaoDoClaude.Confirma || amostra.DecisaoDoClaude == DecisaoDoClaude.Reduz)
            {
                somaErroPosClaude += erroDixonColes * erroDixonColes;
                amostraPosClaude++;
            }
        }

        decimal brierPoissonPuro = amostraTotal == 0 ? 0m : somaErroPoissonPuro / amostraTotal;
        decimal brierDixonColes = amostraTotal == 0 ? 0m : somaErroDixonColes / amostraTotal;
        decimal? brierPosClaude = amostraPosClaude == 0 ? null : somaErroPosClaude / amostraPosClaude;

        return new ResultadoDaMedicao(amostraTotal, brierPoissonPuro, brierDixonColes, amostraPosClaude, brierPosClaude);
    }
}

public sealed record AmostraDeMedicao(decimal ProbPoissonPura, decimal ProbDixonColes, decimal ResultadoReal, DecisaoDoClaude DecisaoDoClaude);

public sealed record ResultadoDaMedicao(
    int AmostraTotal,
    decimal BrierPoissonPuro,
    decimal BrierDixonColes,
    int AmostraPosClaude,
    decimal? BrierPosClaude);
