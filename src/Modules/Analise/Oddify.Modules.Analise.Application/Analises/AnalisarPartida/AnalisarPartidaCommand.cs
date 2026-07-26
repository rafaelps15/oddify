using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Analise.Application.Analises.AnalisarPartida;

public sealed record AnalisarPartidaCommand(Guid PartidaId, string Mercado) : ICommand<Guid>;
