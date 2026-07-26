using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.LiquidarMultipla;

public sealed record LiquidarMultiplaCommand(Guid ApostaMultiplaId) : ICommand;
