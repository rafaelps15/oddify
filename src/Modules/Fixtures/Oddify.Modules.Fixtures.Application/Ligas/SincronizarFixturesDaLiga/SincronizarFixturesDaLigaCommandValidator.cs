using FluentValidation;

namespace Oddify.Modules.Fixtures.Application.Ligas.SincronizarFixturesDaLiga;

internal sealed class SincronizarFixturesDaLigaCommandValidator : AbstractValidator<SincronizarFixturesDaLigaCommand>
{
    public SincronizarFixturesDaLigaCommandValidator()
    {
        RuleFor(c => c.LigaId).NotEmpty().WithMessage("O identificador da liga é obrigatório");
        RuleFor(c => c.Temporada).GreaterThan(2000).WithMessage("A temporada deve ser maior que 2000");
    }
}
