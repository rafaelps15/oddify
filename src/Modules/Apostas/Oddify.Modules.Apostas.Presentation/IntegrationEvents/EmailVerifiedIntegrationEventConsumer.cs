using MassTransit;
using Oddify.Modules.Apostas.Application.Abstractions.Data;
using Oddify.Modules.Apostas.Domain.Bancas;
using Oddify.Modules.Users.IntegrationEvents;

namespace Oddify.Modules.Apostas.Presentation.IntegrationEvents;

public sealed class EmailVerifiedIntegrationEventConsumer(IBancaRepository bancaRepository, IUnitOfWork unitOfWork)
    : IConsumer<EmailVerifiedIntegrationEvent>
{
    private const string NomeDaBancaInicial = "Banca principal";
    private const decimal PercentualPorEntradaPadrao = 0.05m;
    private const PerfilDeRisco PerfilDeRiscoPadrao = PerfilDeRisco.Moderado;

    public async Task Consume(ConsumeContext<EmailVerifiedIntegrationEvent> context)
    {
        EmailVerifiedIntegrationEvent evento = context.Message;

        // Idempotência: se o OutboxProcessorJob do módulo Users republicar esta mesma mensagem
        // (retry após falha ao marcar como processada, por exemplo), sem esse check criaria uma
        // segunda banca pro mesmo usuário.
        bool jaTemBanca = await bancaRepository.ExistsForUsuarioAsync(evento.UserId, context.CancellationToken);
        if (jaTemBanca)
        {
            return;
        }

        var banca = Banca.Create(
            evento.UserId,
            NomeDaBancaInicial,
            saldoInicial: 0m,
            PercentualPorEntradaPadrao,
            PerfilDeRiscoPadrao,
            modoPaperTrading: true,
            evento.OccurredOnUtc);

        bancaRepository.Insert(banca);

        await unitOfWork.SaveChangesAsync(context.CancellationToken);
    }
}
