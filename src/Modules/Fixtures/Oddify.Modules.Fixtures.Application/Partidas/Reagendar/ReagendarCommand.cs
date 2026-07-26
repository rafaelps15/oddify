using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Fixtures.Application.Partidas.Reagendar;

public sealed record ReagendarCommand(Guid PartidaId, DateTime NovaDataUtc) : ICommand;
