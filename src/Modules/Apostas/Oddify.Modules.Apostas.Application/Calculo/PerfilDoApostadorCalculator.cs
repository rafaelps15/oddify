using Oddify.Modules.Apostas.Application.Bancas.GetDesempenhoPorMercado;
using Oddify.Modules.Apostas.Application.Bancas.GetPerfilDoApostador;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;

namespace Oddify.Modules.Apostas.Application.Calculo;

internal static class PerfilDoApostadorCalculator
{
    // "Disciplina de stake": % de apostas cuja entrada não passou de 1,5x a unidade sugerida
    // atual - usa a unidade de HOJE (SaldoAtual * PercentualPorEntrada) como referência para o
    // histórico inteiro, já que o valor da unidade em cada aposta passada não fica registrado
    // em nenhum lugar; é uma aproximação, não um valor histórico exato.
    private const decimal ToleranciaDaUnidade = 1.5m;
    private const decimal LimiarDeDisciplinaGestaoBoa = 0.8m;
    private const int MinimoDeApostasParaSinalizarMercado = 2;

    public static PerfilCalculado Calcular(
        IReadOnlyList<ApostaRow> apostasRecentesPrimeiro,
        decimal unidadeSugerida,
        IReadOnlyCollection<DesempenhoResponse> desempenhoPorMercado)
    {
        decimal entradaMedia = apostasRecentesPrimeiro.Count > 0 ? apostasRecentesPrimeiro.Average(a => a.Stake) : 0m;

        decimal? disciplinaDeStake = apostasRecentesPrimeiro.Count > 0
            ? apostasRecentesPrimeiro.Count(a => a.Stake <= ToleranciaDaUnidade * unidadeSugerida) / (decimal)apostasRecentesPrimeiro.Count
            : null;

        (ResultadoDaAposta? sequenciaTipo, int sequenciaQuantidade) = CalcularSequenciaAtual(apostasRecentesPrimeiro);
        int piorSequenciaDeReds = CalcularMaiorSequenciaDeReds(apostasRecentesPrimeiro);
        IReadOnlyCollection<RecomendacaoResponse> recomendacoes = ConstruirRecomendacoes(disciplinaDeStake, desempenhoPorMercado);

        return new PerfilCalculado(entradaMedia, disciplinaDeStake, sequenciaTipo, sequenciaQuantidade, piorSequenciaDeReds, recomendacoes);
    }

    // Mais recente primeiro - a sequência atual é a quantidade de apostas, a partir do início da
    // lista, que compartilham o mesmo resultado da mais recente.
    private static (ResultadoDaAposta? Tipo, int Quantidade) CalcularSequenciaAtual(IReadOnlyList<ApostaRow> apostasRecentesPrimeiro)
    {
        if (apostasRecentesPrimeiro.Count == 0)
        {
            return (null, 0);
        }

        ResultadoDaAposta tipo = apostasRecentesPrimeiro[0].Resultado;
        int quantidade = apostasRecentesPrimeiro.TakeWhile(a => a.Resultado == tipo).Count();

        return (tipo, quantidade);
    }

    // Maior trecho consecutivo de reds na lista — direção não importa aqui, só o tamanho do maior
    // trecho, daí não precisar da ordem "mais recente primeiro" que CalcularSequenciaAtual exige.
    private static int CalcularMaiorSequenciaDeReds(IReadOnlyList<ApostaRow> apostas)
    {
        return apostas
            .Aggregate(
                (Atual: 0, Maior: 0),
                (estado, aposta) =>
                {
                    int atual = aposta.Resultado == ResultadoDaAposta.Perdida ? estado.Atual + 1 : 0;
                    return (atual, Math.Max(estado.Maior, atual));
                })
            .Maior;
    }

    private static List<RecomendacaoResponse> ConstruirRecomendacoes(
        decimal? disciplinaDeStake,
        IReadOnlyCollection<DesempenhoResponse> desempenhoPorMercado)
    {
        var recomendacoes = new List<RecomendacaoResponse>();

        if (disciplinaDeStake >= LimiarDeDisciplinaGestaoBoa)
        {
            recomendacoes.Add(new RecomendacaoResponse(
                "Gestão disciplinada",
                $"{disciplinaDeStake:P0} das suas entradas respeitam a unidade sugerida. Continue assim: constância vale mais que acerto isolado.",
                Positiva: true));
        }

        DesempenhoResponse? melhorMercado = desempenhoPorMercado.Where(d => d.Lucro > 0).MaxBy(d => d.Lucro);
        if (melhorMercado is not null)
        {
            recomendacoes.Add(new RecomendacaoResponse(
                $"Seu melhor mercado: {melhorMercado.Chave}",
                $"{melhorMercado.QuantidadeDeApostas} apostas resolvidas e R$ {melhorMercado.Lucro:N2} de lucro. "
                    + "Dar mais peso ao que você já lê bem costuma ser o caminho mais curto pro ROI.",
                Positiva: true));
        }

        DesempenhoResponse? piorMercado = desempenhoPorMercado
            .Where(d => d.Lucro < 0 && d.QuantidadeDeApostas >= MinimoDeApostasParaSinalizarMercado)
            .MinBy(d => d.Lucro);
        if (piorMercado is not null)
        {
            recomendacoes.Add(new RecomendacaoResponse(
                $"Reavalie o mercado {piorMercado.Chave}",
                $"{piorMercado.QuantidadeDeApostas} apostas resolvidas e R$ {piorMercado.Lucro:N2} de resultado. "
                    + "Vale reduzir o volume aqui ou estudar melhor as entradas antes da próxima.",
                Positiva: false));
        }

        return recomendacoes;
    }
}

internal sealed record PerfilCalculado(
    decimal EntradaMedia,
    decimal? DisciplinaDeStake,
    ResultadoDaAposta? SequenciaAtualTipo,
    int SequenciaAtualQuantidade,
    int PiorSequenciaDeReds,
    IReadOnlyCollection<RecomendacaoResponse> Recomendacoes);
