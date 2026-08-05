using Microsoft.Extensions.Logging;
using Oddify.Common.Application.Emailing;

namespace Oddify.Common.Infrastructure.Emailing;

// Implementação de dev — escreve o e-mail no log em vez de enviar de verdade, pra testar o
// fluxo sem depender de um provedor real. Trocar por SendGrid/SMTP/etc. depois é só registrar
// outra implementação de IEmailSender; nenhuma lógica de negócio muda (mesmo racional de
// IPasswordHasher/PasswordHasher).
internal sealed partial class ConsoleEmailSender(ILogger<ConsoleEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        LogEmail(logger, to, subject, htmlBody);

        return Task.CompletedTask;
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "E-mail para {To} | Assunto: {Subject}\n{Body}")]
    private static partial void LogEmail(ILogger logger, string to, string subject, string body);
}
