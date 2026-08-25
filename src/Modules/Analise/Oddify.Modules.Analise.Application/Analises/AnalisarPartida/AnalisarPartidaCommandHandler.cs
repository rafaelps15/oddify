using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Analise.Application.Abstractions.Data;
using Oddify.Modules.Analise.Application.Calculo;
using Oddify.Modules.Analise.Domain.Analises;
using Oddify.Modules.Analise.Domain.Fixtures;

namespace Oddify.Modules.Analise.Application.Analises.AnalisarPartida;

internal sealed class AnalisarPartidaCommandHandler(
    ILigaRepository ligaRepository,
    IPartidaRepository partidaRepository,
    ICotacaoRepository cotacaoRepository,
    IAnaliseDePartidaRepository analiseRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AnalisarPartidaCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AnalisarPartidaCommand request, CancellationToken cancellationToken)
    {
        AnaliseCalculada? calculo = await AnaliseDePartidaFactory.ObterCalculoAsync(
            request.PartidaId, request.Mercado, ligaRepository, partidaRepository, cotacaoRepository, cancellationToken);

        if (calculo is null)
        {
            return Result.Failure<Guid>(AnaliseDePartidaErrors.DadosIndisponiveis(request.PartidaId));
        }

        AnaliseDePartida? analiseExistente =
            await analiseRepository.GetPorPartidaEMercadoAsync(request.PartidaId, request.Mercado, cancellationToken);

        Guid analiseId;

        if (analiseExistente is not null)
        {
            analiseExistente.AtualizarCalculo(
                calculo.ProbPoissonPura,
                calculo.ProbDixonColes,
                calculo.ProbImplicitaDaOdd,
                calculo.Vantagem,
                calculo.Odd,
                calculo.Aprovada,
                calculo.Motivo,
                DateTime.UtcNow);

            analiseId = analiseExistente.Id;
        }
        else
        {
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
            analiseId = analise.Id;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return analiseId;
    }
}
