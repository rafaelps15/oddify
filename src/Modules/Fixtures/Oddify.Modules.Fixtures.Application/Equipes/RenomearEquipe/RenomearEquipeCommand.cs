using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Fixtures.Application.Equipes.RenomearEquipe;

public sealed record RenomearEquipeCommand(Guid EquipeId, string Nome) : ICommand;
