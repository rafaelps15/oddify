using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Fixtures.Application.EstatisticasDeEquipe.RegistrarEstatisticaEquipe;

public sealed record RegistrarEstatisticaEquipeCommand(
    Guid PartidaId,
    Guid EquipeId,
    int Gols,
    int Finalizacoes,
    int Escanteios,
    decimal Posse) : ICommand<Guid>;
