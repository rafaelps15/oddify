using FluentValidation;

namespace Oddify.Modules.Analise.Application.Analises.AvaliarComClaude;

internal sealed class AvaliarComClaudeCommandValidator : AbstractValidator<AvaliarComClaudeCommand>
{
    public AvaliarComClaudeCommandValidator()
    {
        RuleFor(c => c.AnaliseId).NotEmpty();
    }
}
