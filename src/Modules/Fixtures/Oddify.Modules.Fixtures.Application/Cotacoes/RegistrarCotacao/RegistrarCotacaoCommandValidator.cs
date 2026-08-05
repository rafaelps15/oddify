using FluentValidation;

namespace Oddify.Modules.Fixtures.Application.Cotacoes.RegistrarCotacao;

internal sealed class RegistrarCotacaoCommandValidator : AbstractValidator<RegistrarCotacaoCommand>
{
    public RegistrarCotacaoCommandValidator()
    {
        RuleFor(c => c.PartidaId).NotEmpty().WithMessage("O identificador da partida é obrigatório");
        RuleFor(c => c.Mercado).NotEmpty().WithMessage("O mercado é obrigatório")
            .MaximumLength(100).WithMessage("O mercado deve ter no máximo 100 caracteres");
        RuleFor(c => c.Odd).GreaterThan(0).WithMessage("A odd deve ser maior que zero");
        RuleFor(c => c.Casa).NotEmpty().WithMessage("A casa de apostas é obrigatória")
            .MaximumLength(100).WithMessage("A casa de apostas deve ter no máximo 100 caracteres");
        RuleFor(c => c.ColetadaEmUtc).NotEmpty().WithMessage("A data de coleta é obrigatória");
    }
}
