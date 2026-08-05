using FluentValidation;

namespace Oddify.Modules.Fixtures.Application.Partidas.CriarPartida;

internal sealed class CriarPartidaCommandValidator : AbstractValidator<CriarPartidaCommand>
{
    public CriarPartidaCommandValidator()
    {
        RuleFor(c => c.IdExterno).NotEmpty().WithMessage("O identificador externo é obrigatório")
            .MaximumLength(100).WithMessage("O identificador externo deve ter no máximo 100 caracteres");
        RuleFor(c => c.LigaId).NotEmpty().WithMessage("O identificador da liga é obrigatório");
        RuleFor(c => c.EquipeCasaId).NotEmpty().WithMessage("O identificador da equipe mandante é obrigatório");
        RuleFor(c => c.EquipeVisitanteId).NotEmpty().WithMessage("O identificador da equipe visitante é obrigatório");
        RuleFor(c => c.DataUtc).NotEmpty().WithMessage("A data da partida é obrigatória");
        RuleFor(c => c.Rodada).GreaterThan(0).WithMessage("A rodada deve ser maior que zero");
        RuleFor(c => c.Temporada).InclusiveBetween(2000, 2100).WithMessage("A temporada deve estar entre 2000 e 2100");
    }
}
