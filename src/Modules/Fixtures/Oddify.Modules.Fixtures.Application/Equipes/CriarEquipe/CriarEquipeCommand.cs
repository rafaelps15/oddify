using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Fixtures.Application.Equipes.CriarEquipe;

public sealed record CriarEquipeCommand(string IdExterno, string Nome, Guid LigaId) : ICommand<Guid>;
