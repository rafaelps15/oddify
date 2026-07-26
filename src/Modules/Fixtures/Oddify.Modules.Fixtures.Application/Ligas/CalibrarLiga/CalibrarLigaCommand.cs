using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Fixtures.Application.Ligas.CalibrarLiga;

public sealed record CalibrarLigaCommand(Guid LigaId) : ICommand;
