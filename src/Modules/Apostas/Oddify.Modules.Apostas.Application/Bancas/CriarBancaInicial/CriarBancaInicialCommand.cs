using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Apostas.Application.Bancas.CriarBancaInicial;

public sealed record CriarBancaInicialCommand(Guid UsuarioId, DateTime OcorridoEmUtc) : ICommand;
