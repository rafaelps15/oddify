using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Fixtures.Application.Ligas.AtualizarMediasDaLiga;

public sealed record AtualizarMediasDaLigaCommand(Guid LigaId, decimal MediaDeGols, decimal FatorCasa) : ICommand;
