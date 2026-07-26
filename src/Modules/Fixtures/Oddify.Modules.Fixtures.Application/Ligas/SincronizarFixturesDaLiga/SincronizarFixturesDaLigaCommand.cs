using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Fixtures.Application.Ligas.SincronizarFixturesDaLiga;

public sealed record SincronizarFixturesDaLigaCommand(Guid LigaId, int Temporada) : ICommand;
