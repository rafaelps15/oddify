using MediatR;
using Oddify.Common.Application.Clock;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.ApostasMultiplas.GetResultadosDasPernas;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;
using Oddify.Modules.Apostas.Domain.Bancas;
using Oddify.Modules.Apostas.Domain.MovimentacoesDaBanca;
using Oddify.Modules.Apostas.Domain.PernasDeAposta;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas;

// Passos de liquidação compartilhados entre LiquidarMultiplaCommandHandler (disparado pelo usuário
// via endpoint, uma múltipla por vez) e LiquidarApostasDaPartidaEncerradaCommandHandler (disparado
// pelo PartidaEncerradaIntegrationEvent, em lote). A banca é buscada aqui dentro por
// apostaMultipla.UsuarioId (não por um IUserContext) — pro caller autenticado isso é o mesmo
// usuário da requisição, já que IApostaMultiplaRepository.GetAsync já filtrou por ele; pro caller
// disparado pelo evento não existe usuário autenticado, só o dono da própria aposta. Nenhum dos
// dois handlers chama SaveChangesAsync aqui dentro, isso continua responsabilidade de cada um
// (mesma regra do CLAUDE.md §7).
public sealed class ApostaMultiplaLiquidacaoService(
    IPernaDeApostaRepository pernaDeApostaRepository,
    IBancaRepository bancaRepository,
    IMovimentacaoDaBancaRepository movimentacaoDaBancaRepository,
    ISender sender,
    IDateTimeProvider dateTimeProvider)
{
    public async Task<Result> LiquidarAsync(ApostaMultipla apostaMultipla, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<PernaDeAposta> pernas =
            await pernaDeApostaRepository.GetPorApostaMultiplaAsync(apostaMultipla.Id, cancellationToken);

        var query = new GetResultadosDasPernasQuery(
            [.. pernas.Select(perna => new PernaParaResolver(perna.Id, perna.PartidaId, perna.Mercado))]);

        Result<IReadOnlyDictionary<Guid, bool>> resultados = await sender.Send(query, cancellationToken);
        if (resultados.IsFailure)
        {
            return Result.Failure(resultados.Error);
        }

        var resultadosDasPernas = new List<bool>();

        foreach (PernaDeAposta perna in pernas)
        {
            bool ganhouPerna = resultados.Value[perna.Id];

            Result resolverResult = perna.Resolver(ganhouPerna);
            if (resolverResult.IsFailure)
            {
                return resolverResult;
            }

            resultadosDasPernas.Add(ganhouPerna);
        }

        bool multiplaGanhou = resultadosDasPernas.Count > 0 && resultadosDasPernas.TrueForAll(ganhou => ganhou);

        DateTime agora = dateTimeProvider.UtcNow;

        Result liquidarResult = apostaMultipla.Liquidar(multiplaGanhou, agora);
        if (liquidarResult.IsFailure)
        {
            return liquidarResult;
        }

        Banca? banca = await bancaRepository.GetAsync(apostaMultipla.BancaId, apostaMultipla.UsuarioId, cancellationToken);
        if (banca is null)
        {
            return Result.Failure(BancaErrors.NotFound(apostaMultipla.BancaId));
        }

        MovimentacaoDaBanca movimentacao = banca.RegistrarMovimentacao(
            apostaMultipla.LucroOuPerda!.Value, TipoDeMovimentacao.Liquidacao, apostaMultipla.Id, agora);
        movimentacaoDaBancaRepository.Insert(movimentacao);

        return Result.Success();
    }
}
