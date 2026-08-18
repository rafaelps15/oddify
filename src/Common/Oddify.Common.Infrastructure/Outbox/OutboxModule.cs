namespace Oddify.Common.Infrastructure.Outbox;

// Um por módulo que publica outbox — registrado por AddOutboxProcessor (chamado de dentro do
// próprio composition root do módulo, não montado como array em Program.cs), resolvido como
// IEnumerable<OutboxModule> só pra OutboxCleanupBackgroundService saber quais schemas limpar.
public sealed record OutboxModule(string Schema);
