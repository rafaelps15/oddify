using FluentValidation;

namespace Oddify.Modules.Fixtures.Application.Ligas.SincronizarFixturesDaLiga;

internal sealed class SincronizarFixturesDaLigaCommandValidator : AbstractValidator<SincronizarFixturesDaLigaCommand>
{
    public SincronizarFixturesDaLigaCommandValidator()
    {
        RuleFor(c => c.LigaId).NotEmpty();
        RuleFor(c => c.Temporada).GreaterThan(2000);
    }
}
