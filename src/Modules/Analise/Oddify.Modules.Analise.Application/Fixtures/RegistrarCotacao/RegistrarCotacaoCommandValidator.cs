using FluentValidation;
using Oddify.Modules.Analise.Domain.Analises;

namespace Oddify.Modules.Analise.Application.Fixtures.RegistrarCotacao;

internal sealed class RegistrarCotacaoCommandValidator : AbstractValidator<RegistrarCotacaoCommand>
{
    public RegistrarCotacaoCommandValidator()
    {
        RuleFor(c => c.CotacaoId).NotEmpty();
        RuleFor(c => c.PartidaId).NotEmpty();
        RuleFor(c => c.Mercado).NotEmpty()
            .Must(MercadoResolver.EhConhecido).WithMessage(c => $"Mercado desconhecido: {c.Mercado}");
        RuleFor(c => c.Odd).GreaterThan(1.0m);
        RuleFor(c => c.Casa).NotEmpty();
    }
}
