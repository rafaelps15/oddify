namespace Oddify.Modules.Apostas.Application.Bancas.GetSugestaoDeStake;

public sealed record SugestaoDeStakeResponse(
    decimal ProbabilidadeImplicita,
    decimal Vantagem,
    decimal StakeSugerido,
    // Expostos pra transparência na UI (ex.: "usando 25% de Kelly, teto de 5% da banca") — são
    // as mesmas constantes que MontarMultiplaCommandHandler usa de verdade ao montar uma aposta,
    // não um valor ajustável pelo usuário (ver KellyCalculator).
    decimal FracaoDeKelly,
    decimal TetoDeStakeSobreABanca);
