using Oddify.Common.Application.Authentication;
using Oddify.Common.Application.Clock;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.Abstractions.Data;
using Oddify.Modules.Apostas.Application.Calculo;
using Oddify.Modules.Apostas.Domain.Bancas;
using Oddify.Modules.Apostas.Domain.JornadasDeAlavancagem;

namespace Oddify.Modules.Apostas.Application.JornadasDeAlavancagem.IniciarJornada;

internal sealed class IniciarJornadaCommandHandler(
    IJornadaDeAlavancagemRepository jornadaDeAlavancagemRepository,
    IFaixaDeMetaCatalogoRepository faixaDeMetaCatalogoRepository,
    IBancaRepository bancaRepository,
    IUnitOfWork unitOfWork,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<IniciarJornadaCommand, Guid>
{
    public async Task<Result<Guid>> Handle(IniciarJornadaCommand request, CancellationToken cancellationToken)
    {
        JornadaDeAlavancagem? jornadaEmAndamento =
            await jornadaDeAlavancagemRepository.GetEmAndamentoAsync(userContext.UserId, cancellationToken);
        if (jornadaEmAndamento is not null)
        {
            return Result.Failure<Guid>(JornadaDeAlavancagemErrors.JaTemJornadaEmAndamento(userContext.UserId));
        }

        FaixaDeMetaCatalogo? catalogo = await faixaDeMetaCatalogoRepository.GetAsync(request.FaixaMeta, cancellationToken);
        if (catalogo is null)
        {
            return Result.Failure<Guid>(JornadaDeAlavancagemErrors.FaixaDeMetaCatalogoNaoEncontrado(request.FaixaMeta));
        }

        decimal bancaMinima = RegrasDeAlavancagem.CalcularBancaMinima(catalogo.NumeroDeFracoes);

        if (request.ValorInicial < bancaMinima)
        {
            return Result.Failure<Guid>(JornadaDeAlavancagemErrors.ValorInicialAbaixoDoMinimo);
        }

        DateTime agora = dateTimeProvider.UtcNow;

        // Banca separada da principal (FinalidadeDaBanca.Alavancagem) — o saldo da jornada nunca
        // se mistura com a gestão de banca normal do usuário. PercentualPorEntrada aqui é só
        // informativo (ValorDaUnidade não é usado pelo módulo de Alavancagem, que dimensiona pelo
        // número de frações da faixa, não por Kelly) — reflete a fração de uma entrada sobre o
        // passo (1/NumeroDeFracoes).
        var banca = Banca.Create(
            userContext.UserId,
            $"Alavancagem — {request.FaixaMeta}",
            request.ValorInicial,
            percentualPorEntrada: 1m / catalogo.NumeroDeFracoes,
            PerfilDeRisco.Agressivo,
            modoPaperTrading: false,
            FinalidadeDaBanca.Alavancagem,
            agora);

        bancaRepository.Insert(banca);

        decimal valorObjetivo = request.ValorInicial * catalogo.Multiplicador;
        decimal probabilidadeDeConclusao =
            RegrasDeAlavancagem.CalcularProbabilidadeDeConclusao(catalogo.NumeroDeFracoes, catalogo.TotalDePassos);

        var jornada = JornadaDeAlavancagem.Create(
            userContext.UserId,
            banca.Id,
            request.FaixaMeta,
            request.ValorInicial,
            valorObjetivo,
            catalogo.NumeroDeFracoes,
            catalogo.TotalDePassos,
            probabilidadeDeConclusao,
            agora);

        jornadaDeAlavancagemRepository.Insert(jornada);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return jornada.Id;
    }
}
