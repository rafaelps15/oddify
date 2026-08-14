using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.AnularApostaMultipla;

public sealed record AnularApostaMultiplaCommand(Guid ApostaMultiplaId) : ICommand;
