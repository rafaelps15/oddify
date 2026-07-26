using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Fixtures.Application.Partidas.RegistrarResultado;

public sealed record RegistrarResultadoCommand(Guid PartidaId, int GolsCasa, int GolsVisitante) : ICommand;
