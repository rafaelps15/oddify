using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Fixtures.Application.Cotacoes.RegistrarCotacao;

public sealed record RegistrarCotacaoCommand(Guid PartidaId, string Mercado, decimal Odd, string Casa, DateTime ColetadaEmUtc)
    : ICommand<Guid>;
