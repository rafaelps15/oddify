using MediatR;
using Oddify.Common.Application.Clock;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.Abstractions.Data;
using Oddify.Modules.Apostas.Application.ApostasMultiplas.GetResultadosDasPernas;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas.Policies;
using Oddify.Modules.Apostas.Domain.Bancas;
using Oddify.Modules.Apostas.Domain.MovimentacoesDaBanca;
using Oddify.Modules.Apostas.Domain.PernasDeAposta;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.LiquidarMultipla;

// Único dono da liquidação de uma ApostaMultipla — o endpoint (usuário autenticado) e
// LiquidarApostasDaPartidaEncerradaCommandHandler (lote, disparado pelo PartidaEncerradaIntegrationEvent)
// convergem para este mesmo Command em vez de compartilhar uma classe/serviço de orquestração à parte
// (mesmo padrão do modular-monolith-with-ddd: reenviar o Command, não extrair um Service). A autorização
// não é mais um filtro embutido no repository (GetAsync(id, usuarioId)) porque o disparo em lote não tem
// usuário autenticado — em vez disso é uma checagem explícita aqui contra o UsuarioId que o próprio
// Command carrega (ver comentário em LiquidarMultiplaCommand).
internal sealed class LiquidarMultiplaCommandHandler(
    IApostaMultiplaRepository apostaMultiplaRepository,
    IPernaDeApostaRepository pernaDeApostaRepository,
    IBancaRepository bancaRepository,
    IMovimentacaoDaBancaRepository movimentacaoDaBancaRepository,
    ISender sender,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<LiquidarMultiplaCommand>
{
    public async Task<Result> Handle(LiquidarMultiplaCommand request, CancellationToken cancellationToken)
    {
        ApostaMultipla? apostaMultipla = await apostaMultiplaRepository.GetByIdAsync(request.ApostaMultiplaId, cancellationToken);
        if (apostaMultipla is null || apostaMultipla.UsuarioId != request.UsuarioId)
        {
            return Result.Failure(ApostaMultiplaErrors.NotFound(request.ApostaMultiplaId));
        }

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

        bool multiplaGanhou = ApostaMultiplaResultadoPolicy.Ganhou(resultadosDasPernas);

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

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
