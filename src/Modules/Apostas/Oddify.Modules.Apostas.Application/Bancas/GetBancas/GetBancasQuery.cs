using Oddify.Common.Application.Messaging;
using Oddify.Modules.Apostas.Application.Bancas.GetBanca;

namespace Oddify.Modules.Apostas.Application.Bancas.GetBancas;

public sealed record GetBancasQuery : IQuery<IReadOnlyCollection<BancaResponse>>;
