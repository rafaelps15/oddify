using Oddify.Common.Application.Messaging;
using Oddify.Modules.Fixtures.Application.Equipes.GetEquipe;

namespace Oddify.Modules.Fixtures.Application.Equipes.GetEquipes;

public sealed record GetEquipesQuery(Guid LigaId) : IQuery<IReadOnlyCollection<EquipeResponse>>;
