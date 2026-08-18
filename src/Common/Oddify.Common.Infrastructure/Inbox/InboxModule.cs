namespace Oddify.Common.Infrastructure.Inbox;

// Um por módulo que CONSOME integration event de outro módulo — registrado por AddInboxProcessor
// (chamado de dentro do próprio composition root do módulo), resolvido como IEnumerable<InboxModule>
// só pra OutboxCleanupBackgroundService saber quais schemas limpar.
public sealed record InboxModule(string Schema);
