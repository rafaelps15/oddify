using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Apostas.Application.Bancas.GetMovimentacoesDaBanca;

public sealed record GetMovimentacoesDaBancaQuery(Guid BancaId, int Page = 1, int PageSize = 15)
    : IQuery<GetMovimentacoesDaBancaResponse>;
