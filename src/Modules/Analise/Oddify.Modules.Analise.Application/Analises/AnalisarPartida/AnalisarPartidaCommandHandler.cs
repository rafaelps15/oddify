using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Analise.Application.Abstractions.Data;
using Oddify.Modules.Analise.Application.Abstractions.Fixtures;
using Oddify.Modules.Analise.Application.Calculo;
using Oddify.Modules.Analise.Domain.Analises;

namespace Oddify.Modules.Analise.Application.Analises.AnalisarPartida;

internal sealed class AnalisarPartidaCommandHandler(
    IAnaliseDePartidaDadosService dadosService,
    IAnaliseDePartidaRepository analiseRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AnalisarPartidaCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AnalisarPartidaCommand request, CancellationToken cancellationToken)
    {
        AnaliseCalculada? calculo = await dadosService.ObterAsync(request.PartidaId, request.Mercado, cancellationToken);

        if (calculo is null)
        {
            return Result.Failure<Guid>(AnaliseDePartidaErrors.DadosIndisponiveis(request.PartidaId));
        }

        var analise = AnaliseDePartida.Create(
            request.PartidaId,
            request.Mercado,
            calculo.ProbPoissonPura,
            calculo.ProbDixonColes,
            calculo.ProbImplicitaDaOdd,
            calculo.Vantagem,
            calculo.Odd,
            calculo.Aprovada,
            calculo.Motivo,
            DateTime.UtcNow);

        analiseRepository.Insert(analise);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return analise.Id;
    }
}
