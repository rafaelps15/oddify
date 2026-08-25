using MediatR;
using Oddify.Common.Application.Authentication;
using Oddify.Common.Application.Clock;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.Abstractions.Data;
using Oddify.Modules.Apostas.Application.JornadasDeAlavancagem.GetOportunidadesParaAlavancagem;
using Oddify.Modules.Apostas.Domain.AnalisesDisponiveis;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;
using Oddify.Modules.Apostas.Domain.Bancas;
using Oddify.Modules.Apostas.Domain.JornadasDeAlavancagem;
using Oddify.Modules.Apostas.Domain.PernasDeAposta;
using Oddify.Modules.Apostas.Domain.PassosDaJornada;

namespace Oddify.Modules.Apostas.Application.JornadasDeAlavancagem.MontarPassoDaJornada;

internal sealed class MontarPassoDaJornadaCommandHandler(
    IJornadaDeAlavancagemRepository jornadaDeAlavancagemRepository,
    IBancaRepository bancaRepository,
    IPassoDaJornadaRepository passoDaJornadaRepository,
    IAnaliseDisponivelParaApostaRepository analiseDisponivelRepository,
    IApostaMultiplaRepository apostaMultiplaRepository,
    IPernaDeApostaRepository pernaDeApostaRepository,
    ISender sender,
    IUnitOfWork unitOfWork,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<MontarPassoDaJornadaCommand, Guid>
{
    public async Task<Result<Guid>> Handle(MontarPassoDaJornadaCommand request, CancellationToken cancellationToken)
    {
        JornadaDeAlavancagem? jornada =
            await jornadaDeAlavancagemRepository.GetAsync(request.JornadaId, userContext.UserId, cancellationToken);
        if (jornada is null)
        {
            return Result.Failure<Guid>(JornadaDeAlavancagemErrors.NotFound(request.JornadaId));
        }

        if (jornada.Status != StatusDaJornada.EmAndamento)
        {
            return Result.Failure<Guid>(JornadaDeAlavancagemErrors.NaoEstaEmAndamento(jornada.Id));
        }

        Banca? banca = await bancaRepository.GetAsync(jornada.BancaId, userContext.UserId, cancellationToken);
        if (banca is null)
        {
            return Result.Failure<Guid>(BancaErrors.NotFound(jornada.BancaId));
        }

        Result<IReadOnlyCollection<OportunidadeParaAlavancagemResponse>> oportunidadesResult =
            await sender.Send(new GetOportunidadesParaAlavancagemQuery(jornada.NumeroDeFracoes), cancellationToken);
        if (oportunidadesResult.IsFailure)
        {
            return Result.Failure<Guid>(oportunidadesResult.Error);
        }

        IReadOnlyCollection<OportunidadeParaAlavancagemResponse> oportunidades = oportunidadesResult.Value;
        if (oportunidades.Count < jornada.NumeroDeFracoes)
        {
            return Result.Failure<Guid>(PassoDaJornadaErrors.SemOportunidadesSuficientes);
        }

        DateTime agora = dateTimeProvider.UtcNow;
        decimal valorDoPasso = banca.SaldoAtual;
        decimal valorPorFracao = valorDoPasso / jornada.NumeroDeFracoes;

        var passo = PassoDaJornada.Create(jornada.Id, jornada.PassoAtual, valorDoPasso, jornada.NumeroDeFracoes, agora);
        passoDaJornadaRepository.Insert(passo);

        foreach (OportunidadeParaAlavancagemResponse oportunidade in oportunidades)
        {
            AnaliseDisponivelParaAposta? analiseDisponivel =
                await analiseDisponivelRepository.GetAsync(oportunidade.Id, cancellationToken);
            if (analiseDisponivel is null)
            {
                return Result.Failure<Guid>(AnaliseDisponivelParaApostaErrors.NotFound(oportunidade.Id));
            }

            Result marcarResult = analiseDisponivel.MarcarComoUtilizada();
            if (marcarResult.IsFailure)
            {
                return Result.Failure<Guid>(marcarResult.Error);
            }

            var apostaMultipla = ApostaMultipla.Create(
                userContext.UserId,
                banca.Id,
                oportunidade.OddDeMercado,
                valorPorFracao,
                OrigemDaAposta.Alavancagem,
                descricao: oportunidade.Mercado,
                passo.Id,
                agora);
            apostaMultiplaRepository.Insert(apostaMultipla);

            var perna = PernaDeAposta.Create(
                apostaMultipla.Id, oportunidade.Id, oportunidade.PartidaId, oportunidade.Mercado, oportunidade.OddDeMercado);
            pernaDeApostaRepository.Insert(perna);
        }

        Result marcarEmAbertoResult = passo.MarcarEmAberto();
        if (marcarEmAbertoResult.IsFailure)
        {
            return Result.Failure<Guid>(marcarEmAbertoResult.Error);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return passo.Id;
    }
}
