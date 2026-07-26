using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Fixtures.Application.Partidas.CriarPartida;

public sealed record CriarPartidaCommand(
    string IdExterno,
    Guid LigaId,
    Guid EquipeCasaId,
    Guid EquipeVisitanteId,
    DateTime DataUtc) : ICommand<Guid>;
