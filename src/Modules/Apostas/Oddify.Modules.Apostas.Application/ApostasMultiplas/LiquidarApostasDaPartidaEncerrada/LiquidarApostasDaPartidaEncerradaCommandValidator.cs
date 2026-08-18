using FluentValidation;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.LiquidarApostasDaPartidaEncerrada;

internal sealed class LiquidarApostasDaPartidaEncerradaCommandValidator : AbstractValidator<LiquidarApostasDaPartidaEncerradaCommand>
{
    public LiquidarApostasDaPartidaEncerradaCommandValidator()
    {
        RuleFor(c => c.PartidaId).NotEmpty().WithMessage("O identificador da partida é obrigatório");
    }
}
