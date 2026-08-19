using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.LiquidarMultipla;

// UsuarioId vai explícito no Command (não via IUserContext dentro do Handler) porque esse Command
// tem dois disparadores: o endpoint (usuário autenticado) e LiquidarApostasDaPartidaEncerradaCommandHandler
// (reage em lote ao PartidaEncerradaIntegrationEvent, sem usuário autenticado na requisição — só
// reenvia este mesmo Command usando o UsuarioId que já carrega de cada ApostaMultipla). Mesmo padrão
// de CriarBancaInicialCommand, que carrega UsuarioId porque é disparado por um integration event.
public sealed record LiquidarMultiplaCommand(Guid ApostaMultiplaId, Guid UsuarioId) : ICommand;
