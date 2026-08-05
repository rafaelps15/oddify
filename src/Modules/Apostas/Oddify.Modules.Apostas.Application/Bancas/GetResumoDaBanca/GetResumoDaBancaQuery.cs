using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Apostas.Application.Bancas.GetResumoDaBanca;

public sealed record GetResumoDaBancaQuery(Guid BancaId) : IQuery<ResumoDaBancaResponse>;
