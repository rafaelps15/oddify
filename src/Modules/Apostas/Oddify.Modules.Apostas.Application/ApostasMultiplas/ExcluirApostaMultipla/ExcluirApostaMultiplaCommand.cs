using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.ExcluirApostaMultipla;

public sealed record ExcluirApostaMultiplaCommand(Guid ApostaMultiplaId) : ICommand;
