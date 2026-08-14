using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Apostas.Application.Bancas.GetDistribuicaoDeResultados;

public sealed record GetDistribuicaoDeResultadosQuery(Guid BancaId) : IQuery<DistribuicaoDeResultadosResponse>;
