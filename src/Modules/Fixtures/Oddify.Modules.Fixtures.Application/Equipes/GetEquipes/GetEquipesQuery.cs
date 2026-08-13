using Oddify.Common.Application.Messaging;
using Oddify.Modules.Fixtures.Application.Equipes.GetEquipe;

namespace Oddify.Modules.Fixtures.Application.Equipes.GetEquipes;

// LigaId e Ids são independentes e combinam via AND (ambos null-safe) — o uso real é sempre um ou
// outro: a tela de Equipes manda só LigaId, a resolução de nomes de time a partir de pernas de
// aposta manda só Ids.
public sealed record GetEquipesQuery(Guid? LigaId, Guid[]? Ids = null) : IQuery<IReadOnlyCollection<EquipeResponse>>;
