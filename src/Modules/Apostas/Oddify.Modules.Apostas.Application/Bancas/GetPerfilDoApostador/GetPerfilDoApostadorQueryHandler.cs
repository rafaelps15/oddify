using System.Data.Common;
using Dapper;
using MediatR;
using Oddify.Common.Application.Authentication;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.Bancas.GetDesempenhoPorMercado;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;
using Oddify.Modules.Apostas.Domain.Bancas;

namespace Oddify.Modules.Apostas.Application.Bancas.GetPerfilDoApostador;

internal sealed class GetPerfilDoApostadorQueryHandler(IDbConnectionFactory dbConnectionFactory, ISender sender, IUserContext userContext)
    : IQueryHandler<GetPerfilDoApostadorQuery, PerfilDoApostadorResponse>
{
    // "Disciplina de stake": % de apostas cuja entrada não passou de 1,5x a unidade sugerida
    // atual - usa a unidade de HOJE (SaldoAtual * PercentualPorEntrada) como referência para o
    // histórico inteiro, já que o valor da unidade em cada aposta passada não fica registrado
    // em nenhum lugar; é uma aproximação, não um valor histórico exato.
    private const decimal ToleranciaDaUnidade = 1.5m;
    private const decimal LimiarDeDisciplinaGestaoBoa = 0.8m;
    private const int MinimoDeApostasParaSinalizarMercado = 2;

    public async Task<Result<PerfilDoApostadorResponse>> Handle(GetPerfilDoApostadorQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        var parametros = new PerfilParametros(request.BancaId, userContext.UserId);

        BancaRow? banca = await connection.QuerySingleOrDefaultAsync<BancaRow>(
            $"""
             SELECT
                 saldo_atual AS {nameof(BancaRow.SaldoAtual)},
                 percentual_por_entrada AS {nameof(BancaRow.PercentualPorEntrada)}
             FROM apostas.bancas
             WHERE id = @BancaId AND usuario_id = @UsuarioId
             """,
            parametros);

        if (banca is null)
        {
            return Result.Failure<PerfilDoApostadorResponse>(BancaErrors.NotFound(request.BancaId));
        }

        decimal unidadeSugerida = banca.SaldoAtual * banca.PercentualPorEntrada;

        // Mais recente primeiro - a sequência atual é lida a partir do início desta lista; a pior
        // sequência de reds é a mesma lista, direção não importa pra achar o maior trecho consecutivo.
        List<ApostaRow> apostas = (await connection.QueryAsync<ApostaRow>(
            $"""
             SELECT
                 resultado AS {nameof(ApostaRow.Resultado)},
                 stake AS {nameof(ApostaRow.Stake)}
             FROM apostas.apostas_multiplas
             WHERE banca_id = @BancaId AND usuario_id = @UsuarioId AND resultado IN (1, 2)
             ORDER BY atualizado_em_utc DESC
             """,
            parametros)).AsList();

        decimal entradaMedia = apostas.Count > 0 ? apostas.Average(a => a.Stake) : 0m;

        decimal? disciplinaDeStake = apostas.Count > 0
            ? apostas.Count(a => a.Stake <= ToleranciaDaUnidade * unidadeSugerida) / (decimal)apostas.Count
            : null;

        (ResultadoDaAposta? sequenciaTipo, int sequenciaQuantidade) = CalcularSequenciaAtual(apostas);
        int piorSequenciaDeReds = CalcularMaiorSequenciaDeReds(apostas);

        Result<IReadOnlyCollection<DesempenhoResponse>> desempenhoPorMercadoResult =
            await sender.Send(new GetDesempenhoPorMercadoQuery(request.BancaId), cancellationToken);

        IReadOnlyCollection<DesempenhoResponse> desempenhoPorMercado =
            desempenhoPorMercadoResult.IsSuccess ? desempenhoPorMercadoResult.Value : [];

        IReadOnlyCollection<RecomendacaoResponse> recomendacoes =
            ConstruirRecomendacoes(disciplinaDeStake, desempenhoPorMercado);

        return new PerfilDoApostadorResponse(
            request.BancaId,
            entradaMedia,
            unidadeSugerida,
            disciplinaDeStake,
            sequenciaTipo,
            sequenciaQuantidade,
            piorSequenciaDeReds,
            recomendacoes);
    }

    private static (ResultadoDaAposta? Tipo, int Quantidade) CalcularSequenciaAtual(IReadOnlyList<ApostaRow> apostasRecentesPrimeiro)
    {
        if (apostasRecentesPrimeiro.Count == 0)
        {
            return (null, 0);
        }

        ResultadoDaAposta tipo = apostasRecentesPrimeiro[0].Resultado;
        int quantidade = 0;

        foreach (ApostaRow aposta in apostasRecentesPrimeiro)
        {
            if (aposta.Resultado != tipo)
            {
                break;
            }

            quantidade++;
        }

        return (tipo, quantidade);
    }

    private static int CalcularMaiorSequenciaDeReds(IReadOnlyList<ApostaRow> apostas)
    {
        int maior = 0;
        int atual = 0;

        foreach (ApostaRow aposta in apostas)
        {
            atual = aposta.Resultado == ResultadoDaAposta.Perdida ? atual + 1 : 0;
            maior = Math.Max(maior, atual);
        }

        return maior;
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

    private sealed record PerfilParametros(Guid BancaId, Guid UsuarioId);

    private sealed record BancaRow(decimal SaldoAtual, decimal PercentualPorEntrada);

    private sealed record ApostaRow(ResultadoDaAposta Resultado, decimal Stake);
}
