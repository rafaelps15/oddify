using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Apostas.Application.Bancas.GetBanca;

public sealed record GetBancaQuery(Guid BancaId) : IQuery<BancaResponse>;
