using FluentValidation;
using Oddify.Modules.Analise.Domain.Analises;

namespace Oddify.Modules.Analise.Application.Analises.AnalisarPartida;

internal sealed class AnalisarPartidaCommandValidator : AbstractValidator<AnalisarPartidaCommand>
{
    public AnalisarPartidaCommandValidator()
    {
        RuleFor(c => c.PartidaId).NotEmpty().WithMessage("O identificador da partida é obrigatório");
        RuleFor(c => c.Mercado).NotEmpty().WithMessage("O mercado é obrigatório")
            .MaximumLength(100).WithMessage("O mercado deve ter no máximo 100 caracteres")
            .Must(MercadoResolver.EhConhecido).WithMessage(c => $"Mercado desconhecido: {c.Mercado}");
    }
}
