using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Apostas.Application.JornadasDeAlavancagem.AvaliarPassoDaJornada;

public sealed record AvaliarPassoDaJornadaCommand(Guid PassoId) : ICommand;
