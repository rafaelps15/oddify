using FluentValidation;

namespace Oddify.Modules.Fixtures.Application.Escalacoes.RegistrarEscalacao;

internal sealed class RegistrarEscalacaoCommandValidator : AbstractValidator<RegistrarEscalacaoCommand>
{
    public RegistrarEscalacaoCommandValidator()
    {
        RuleFor(c => c.PartidaId).NotEmpty().WithMessage("O identificador da partida é obrigatório");
        RuleFor(c => c.EquipeId).NotEmpty().WithMessage("O identificador da equipe é obrigatório");
        RuleFor(c => c.Formacao).NotEmpty().MaximumLength(20).WithMessage("A formação é obrigatória");
        RuleFor(c => c.Tecnico).NotEmpty().MaximumLength(200).WithMessage("O nome do técnico é obrigatório");
    }
}
