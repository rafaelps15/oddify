using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Apostas.Application.JornadasDeAlavancagem.MontarPassoDaJornada;

public sealed record MontarPassoDaJornadaCommand(Guid JornadaId) : ICommand<Guid>;
