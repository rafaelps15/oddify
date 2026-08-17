using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Analise.Application.Abstractions.Data;
using Oddify.Modules.Analise.Application.Abstractions.Llm;
using Oddify.Modules.Analise.Domain.Analises;

namespace Oddify.Modules.Analise.Application.Analises.AvaliarComClaude;

internal sealed class AvaliarComClaudeCommandHandler(
    IAnaliseDePartidaRepository analiseRepository,
    IClaudeAvaliadorCriticoService claudeService,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AvaliarComClaudeCommand>
{
    internal const string VersaoDoPrompt = "avaliador-critico-v1";

    public async Task<Result> Handle(AvaliarComClaudeCommand request, CancellationToken cancellationToken)
    {
        AnaliseDePartida? analise = await analiseRepository.GetAsync(request.AnaliseId, cancellationToken);
        if (analise is null)
        {
            return Result.Failure(AnaliseDePartidaErrors.NotFound(request.AnaliseId));
        }

        if (!analise.AprovadaNoFiltro)
        {
            return Result.Failure(AnaliseDePartidaErrors.NaoAprovadaNoFiltro(request.AnaliseId));
        }

        var contexto = new AnaliseContexto(
            analise.Id,
            analise.PartidaId,
            analise.Mercado,
            analise.ProbPoissonPura,
            analise.ProbDixonColes,
            analise.ProbImplicitaDaOdd,
            analise.Vantagem,
            analise.OddDeMercado,
            request.ContextoAdicional);

        Result<VeredictoClaude> veredictoResult = await claudeService.AvaliarAsync(contexto, cancellationToken);

        if (veredictoResult.IsFailure)
        {
            // Propaga o erro do serviço em vez de engolir pra Success — o RequestLoggingPipelineBehavior
            // já loga "Completed request ... with error" com o Error completo no contexto estruturado,
            // então não precisa de log manual aqui, e o caller passa a saber que a avaliação falhou em
            // vez de receber um 200 silencioso sem nada ter mudado.
            return Result.Failure(veredictoResult.Error);
        }

        VeredictoClaude veredicto = veredictoResult.Value;

        Result resultado = analise.RegistrarDecisaoDoClaude(veredicto.Decisao, veredicto.Justificativa, veredicto.RespostaBruta, VersaoDoPrompt);
        if (resultado.IsFailure)
        {
            return resultado;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
