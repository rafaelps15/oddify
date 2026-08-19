using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Analise.Application.Fixtures.RegistrarPartida;

public sealed record RegistrarPartidaCommand(
    Guid PartidaId,
    Guid LigaId,
    Guid EquipeCasaId,
    Guid EquipeVisitanteId,
    DateTime DataUtc) : ICommand;
