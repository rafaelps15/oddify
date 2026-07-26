using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Fixtures.Application.Ligas.CriarLiga;

public sealed record CriarLigaCommand(string IdExterno, string Nome, decimal MediaDeGols, decimal FatorCasa) : ICommand<Guid>;
