using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Apostas.Application.JornadasDeAlavancagem.GetJornada;

public sealed record GetJornadaQuery(Guid JornadaId) : IQuery<JornadaResponse>;
