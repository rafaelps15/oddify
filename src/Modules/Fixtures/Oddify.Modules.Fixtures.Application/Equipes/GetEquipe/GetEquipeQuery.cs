using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Fixtures.Application.Equipes.GetEquipe;

public sealed record GetEquipeQuery(Guid EquipeId) : IQuery<EquipeResponse>;
