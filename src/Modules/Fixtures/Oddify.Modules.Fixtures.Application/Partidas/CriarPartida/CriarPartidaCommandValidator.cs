using FluentValidation;

namespace Oddify.Modules.Fixtures.Application.Partidas.CriarPartida;

internal sealed class CriarPartidaCommandValidator : AbstractValidator<CriarPartidaCommand>
{
    public CriarPartidaCommandValidator()
    {
        RuleFor(c => c.IdExterno).NotEmpty().MaximumLength(100);
        RuleFor(c => c.LigaId).NotEmpty();
        RuleFor(c => c.EquipeCasaId).NotEmpty();
        RuleFor(c => c.EquipeVisitanteId).NotEmpty();
        RuleFor(c => c.DataUtc).NotEmpty();
        RuleFor(c => c.Rodada).GreaterThan(0);
        RuleFor(c => c.Temporada).InclusiveBetween(2000, 2100);
    }
}
