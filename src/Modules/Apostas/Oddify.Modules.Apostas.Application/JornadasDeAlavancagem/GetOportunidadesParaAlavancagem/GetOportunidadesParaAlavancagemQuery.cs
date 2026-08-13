using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Apostas.Application.JornadasDeAlavancagem.GetOportunidadesParaAlavancagem;

public sealed record GetOportunidadesParaAlavancagemQuery(int Quantidade)
    : IQuery<IReadOnlyCollection<OportunidadeParaAlavancagemResponse>>;
