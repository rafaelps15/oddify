using FluentValidation;

namespace Oddify.Modules.Fixtures.Application.Cotacoes.RegistrarCotacao;

internal sealed class RegistrarCotacaoCommandValidator : AbstractValidator<RegistrarCotacaoCommand>
{
    public RegistrarCotacaoCommandValidator()
    {
        RuleFor(c => c.PartidaId).NotEmpty();
        RuleFor(c => c.Mercado).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Odd).GreaterThan(0);
        RuleFor(c => c.Casa).NotEmpty().MaximumLength(100);
        RuleFor(c => c.ColetadaEmUtc).NotEmpty();
    }
}
