using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Apostas.Application.Bancas.GetPerfilDoApostador;

public sealed record GetPerfilDoApostadorQuery(Guid BancaId) : IQuery<PerfilDoApostadorResponse>;
