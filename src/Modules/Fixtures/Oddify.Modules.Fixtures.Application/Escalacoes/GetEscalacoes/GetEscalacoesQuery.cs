using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Fixtures.Application.Escalacoes.GetEscalacoes;

public sealed record GetEscalacoesQuery(Guid PartidaId) : IQuery<IReadOnlyCollection<EscalacaoResponse>>;
