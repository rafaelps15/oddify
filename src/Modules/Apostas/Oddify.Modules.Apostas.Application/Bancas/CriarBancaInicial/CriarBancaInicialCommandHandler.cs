using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.Abstractions.Data;
using Oddify.Modules.Apostas.Domain.Bancas;

namespace Oddify.Modules.Apostas.Application.Bancas.CriarBancaInicial;

internal sealed class CriarBancaInicialCommandHandler(IBancaRepository bancaRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<CriarBancaInicialCommand>
{
    private const string NomeDaBancaInicial = "Banca principal";
    private const decimal PercentualPorEntradaPadrao = 0.05m;
    private const PerfilDeRisco PerfilDeRiscoPadrao = PerfilDeRisco.Moderado;

    public async Task<Result> Handle(CriarBancaInicialCommand request, CancellationToken cancellationToken)
    {
        // Idempotência: se o OutboxProcessorJob do módulo Users republicar esta mesma mensagem
        // (retry após falha ao marcar como processada, por exemplo), sem esse check criaria uma
        // segunda banca pro mesmo usuário.
        bool jaTemBanca = await bancaRepository.ExistsForUsuarioAsync(request.UsuarioId, cancellationToken);
        if (jaTemBanca)
        {
            return Result.Success();
        }

        var banca = Banca.Create(
            request.UsuarioId,
            NomeDaBancaInicial,
            saldoInicial: 0m,
            PercentualPorEntradaPadrao,
            PerfilDeRiscoPadrao,
            modoPaperTrading: true,
            FinalidadeDaBanca.Principal,
            request.OcorridoEmUtc);

        bancaRepository.Insert(banca);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
