namespace Oddify.Modules.Apostas.Domain.JornadasDeAlavancagem;

// Cada faixa tem seu próprio número de frações por passo, número de passos até o objetivo e
// probabilidade de conclusão exibida ao usuário — ver FaixaDeMetaCatalogo (Application/Calculo)
// pros números reais de cada uma. Faixas ambiciosas usam mais frações por passo (prioriza
// sobreviver a uma jornada mais longa); faixas curtas usam menos (cresce mais rápido) — spec
// 17.12.
public enum FaixaDeMeta
{
    Dobrar = 0,
    Triplicar = 1,
    CincoVezes = 2
}
