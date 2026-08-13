using Oddify.Common.Application.Messaging;
using Oddify.Modules.Apostas.Domain.JornadasDeAlavancagem;

namespace Oddify.Modules.Apostas.Application.JornadasDeAlavancagem.IniciarJornada;

public sealed record IniciarJornadaCommand(FaixaDeMeta FaixaMeta, decimal ValorInicial) : ICommand<Guid>;
