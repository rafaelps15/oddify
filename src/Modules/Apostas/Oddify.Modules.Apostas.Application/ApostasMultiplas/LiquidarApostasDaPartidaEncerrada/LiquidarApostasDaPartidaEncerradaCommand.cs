using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.LiquidarApostasDaPartidaEncerrada;

public sealed record LiquidarApostasDaPartidaEncerradaCommand(Guid PartidaId) : ICommand;
