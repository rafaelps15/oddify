using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Fixtures.Application.EscalacoesDeJogador.RegistrarEscalacaoJogador;

public sealed record RegistrarEscalacaoJogadorCommand(
    Guid EscalacaoId,
    Guid JogadorId,
    bool Titular,
    string Posicao,
    int? Numero) : ICommand<Guid>;
