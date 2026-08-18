using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Fixtures.Application.Escalacoes.RegistrarEscalacao;

public sealed record RegistrarEscalacaoCommand(
    Guid PartidaId,
    Guid EquipeId,
    string Formacao,
    string Tecnico) : ICommand<Guid>;
