using FluentValidation;

namespace Oddify.Modules.Analise.Application.Fixtures.RegistrarPartida;

internal sealed class RegistrarPartidaCommandValidator : AbstractValidator<RegistrarPartidaCommand>
{
    public RegistrarPartidaCommandValidator()
    {
        RuleFor(c => c.PartidaId).NotEmpty();
        RuleFor(c => c.LigaId).NotEmpty();
        RuleFor(c => c.EquipeCasaId).NotEmpty();
        RuleFor(c => c.EquipeVisitanteId).NotEmpty();
    }
}
