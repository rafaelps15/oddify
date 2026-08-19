using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Analise.Application.Fixtures.UpsertLiga;

public sealed record UpsertLigaCommand(Guid LigaId, string Nome, decimal MediaDeGols, decimal FatorCasa, bool Calibrada) : ICommand;
