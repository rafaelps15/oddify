using FluentValidation;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.RegistrarAnaliseDisponivelParaAposta;

internal sealed class RegistrarAnaliseDisponivelParaApostaCommandValidator
    : AbstractValidator<RegistrarAnaliseDisponivelParaApostaCommand>
{
    public RegistrarAnaliseDisponivelParaApostaCommandValidator()
    {
        RuleFor(c => c.AnaliseId).NotEmpty();
        RuleFor(c => c.PartidaId).NotEmpty();
        RuleFor(c => c.Mercado).NotEmpty();
        RuleFor(c => c.OddDeMercado).GreaterThan(0);
        RuleFor(c => c.ProbabilidadeConfirmada).InclusiveBetween(0m, 1m);
    }
}
