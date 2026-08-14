using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Apostas.Application.Bancas.GetResultadoDiario;

public sealed record GetResultadoDiarioQuery(Guid BancaId, int Ano, int Mes) : IQuery<IReadOnlyCollection<ResultadoDiarioResponse>>;
