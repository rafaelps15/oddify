using Oddify.Common.Application.Authentication;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.Abstractions.Data;
using Oddify.Modules.Apostas.Application.Calculo;
using Oddify.Modules.Apostas.Domain.AnalisesDisponiveis;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;
using Oddify.Modules.Apostas.Domain.Bancas;
using Oddify.Modules.Apostas.Domain.PernasDeAposta;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.MontarMultipla;

internal sealed class MontarMultiplaCommandHandler(
    IBancaRepository bancaRepository,
    IAnaliseDisponivelParaApostaRepository analiseDisponivelRepository,
    IApostaMultiplaRepository apostaMultiplaRepository,
    IPernaDeApostaRepository pernaDeApostaRepository,
    IUnitOfWork unitOfWork,
    IUserContext userContext)
    : ICommandHandler<MontarMultiplaCommand, Guid>
{
    public async Task<Result<Guid>> Handle(MontarMultiplaCommand request, CancellationToken cancellationToken)
    {
        Banca? banca = await bancaRepository.GetAsync(request.BancaId, userContext.UserId, cancellationToken);
        if (banca is null)
        {
            return Result.Failure<Guid>(BancaErrors.NotFound(request.BancaId));
        }

        var disponiveis = new List<AnaliseDisponivelParaAposta>();

        foreach (Guid analiseId in request.AnaliseIds)
        {
            AnaliseDisponivelParaAposta? disponivel = await analiseDisponivelRepository.GetAsync(analiseId, cancellationToken);

            if (disponivel is null)
            {
                return Result.Failure<Guid>(AnaliseDisponivelParaApostaErrors.NotFound(analiseId));
            }

            if (disponivel.JaUtilizada)
            {
                return Result.Failure<Guid>(AnaliseDisponivelParaApostaErrors.JaUtilizada(analiseId));
            }

            disponiveis.Add(disponivel);
        }

        if (disponiveis.Select(d => d.PartidaId).Distinct().Count() != disponiveis.Count)
        {
            return Result.Failure<Guid>(ApostaMultiplaErrors.PartidasRepetidas);
        }

        decimal oddCombinada = disponiveis.Aggregate(1m, (acumulado, item) => acumulado * item.OddDeMercado);
        decimal probabilidadeCombinada = disponiveis.Aggregate(1m, (acumulado, item) => acumulado * item.ProbabilidadeConfirmada);

        decimal stake = KellyCalculator.CalcularStake(banca.SaldoAtual, probabilidadeCombinada, oddCombinada);

        if (disponiveis.Any(d => d.Reduzida))
        {
            stake *= 0.5m;
        }

        if (stake <= 0m)
        {
            return Result.Failure<Guid>(ApostaMultiplaErrors.StakeNulo);
        }

        var apostaMultipla = ApostaMultipla.Create(
            userContext.UserId, request.BancaId, oddCombinada, stake, OrigemDaAposta.ManualEntry, request.Descricao, passoDaJornadaId: null, DateTime.UtcNow);
        apostaMultiplaRepository.Insert(apostaMultipla);

        foreach (AnaliseDisponivelParaAposta disponivel in disponiveis)
        {
            var perna = PernaDeAposta.Create(apostaMultipla.Id, disponivel.Id, disponivel.PartidaId, disponivel.Mercado, disponivel.OddDeMercado);
            pernaDeApostaRepository.Insert(perna);

            Result marcarResult = disponivel.MarcarComoUtilizada();
            if (marcarResult.IsFailure)
            {
                return Result.Failure<Guid>(marcarResult.Error);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return apostaMultipla.Id;
    }
}
