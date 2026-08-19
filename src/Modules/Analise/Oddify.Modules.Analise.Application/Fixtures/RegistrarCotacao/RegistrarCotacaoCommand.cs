using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Analise.Application.Fixtures.RegistrarCotacao;

public sealed record RegistrarCotacaoCommand(
    Guid CotacaoId,
    Guid PartidaId,
    string Mercado,
    decimal Odd,
    string Casa,
    DateTime ColetadaEmUtc) : ICommand;
