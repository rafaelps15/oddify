using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Apostas.Application.JornadasDeAlavancagem.GetFaixasDeMeta;

public sealed record GetFaixasDeMetaQuery : IQuery<IReadOnlyCollection<FaixaDeMetaResponse>>;
