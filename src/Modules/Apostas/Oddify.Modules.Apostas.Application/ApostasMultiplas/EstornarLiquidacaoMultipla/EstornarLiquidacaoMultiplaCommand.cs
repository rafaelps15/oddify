using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.EstornarLiquidacaoMultipla;

public sealed record EstornarLiquidacaoMultiplaCommand(Guid ApostaMultiplaId) : ICommand;
