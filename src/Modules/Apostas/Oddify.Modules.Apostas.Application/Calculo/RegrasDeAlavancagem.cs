namespace Oddify.Modules.Apostas.Application.Calculo;

// Fórmulas do módulo de Alavancagem (spec 17.12) — a spec mestra marca banca mínima, faixas de
// meta e número de frações como "estrutura a definir"; só a matemática de avanço/quebra por
// fração é dada de verdade (p~70% por entrada). O catálogo por faixa (multiplicador/frações/
// passos) mora em FaixaDeMetaCatalogo (Domain, seedado via migration) — fonte única usada tanto
// por IniciarJornadaCommandHandler (via IFaixaDeMetaCatalogoRepository) quanto por
// GetFaixasDeMetaQueryHandler (via Dapper); esta classe só faz a matemática em cima dos números
// já buscados, nunca guarda o catálogo em memória.
internal static class RegrasDeAlavancagem
{
    // Restrição fixa da spec: só entra oportunidade com odd <= 1.50 (maximiza taxa de acerto) e
    // edge real do motor — nunca odd de "favorito qualquer".
    public const decimal OddMaxima = 1.50m;

    // Unidade de entrada fixa — banca mínima de uma faixa = NumeroDeFracoes * UnidadeDeEntrada
    // (mesma base do exemplo da spec: R$75 = 3 x R$25).
    public const decimal UnidadeDeEntrada = 25m;

    // "Matematica com p~70% por entrada" — spec 17.12.
    private const decimal ProbabilidadeBasePorEntrada = 0.70m;

    public static decimal CalcularBancaMinima(int numeroDeFracoes) => numeroDeFracoes * UnidadeDeEntrada;

    // Regra da faixa: pelo menos 2 das N apostas do passo precisam ganhar pra avançar — a mesma
    // regra pra qualquer N é o que faz os dois números da spec (78% pra 3 frações, 91% pra 4)
    // baterem exatamente com P(>=2 de N).
    public static decimal CalcularProbabilidadeDeAvancoPorPasso(int numeroDeFracoes)
    {
        double p = (double)ProbabilidadeBasePorEntrada;
        double q = 1 - p;
        double probabilidadeZeroVitorias = Math.Pow(q, numeroDeFracoes);
        double probabilidadeUmaVitoria = numeroDeFracoes * p * Math.Pow(q, numeroDeFracoes - 1);

        return (decimal)(1 - probabilidadeZeroVitorias - probabilidadeUmaVitoria);
    }

    public static decimal CalcularProbabilidadeDeConclusao(int numeroDeFracoes, int totalDePassos)
    {
        decimal avancoPorPasso = CalcularProbabilidadeDeAvancoPorPasso(numeroDeFracoes);

        return (decimal)Math.Pow((double)avancoPorPasso, totalDePassos);
    }
}
