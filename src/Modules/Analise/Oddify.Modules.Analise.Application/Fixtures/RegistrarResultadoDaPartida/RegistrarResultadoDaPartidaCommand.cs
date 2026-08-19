using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Analise.Application.Fixtures.RegistrarResultadoDaPartida;

public sealed record RegistrarResultadoDaPartidaCommand(Guid PartidaId, int GolsCasa, int GolsVisitante) : ICommand;
