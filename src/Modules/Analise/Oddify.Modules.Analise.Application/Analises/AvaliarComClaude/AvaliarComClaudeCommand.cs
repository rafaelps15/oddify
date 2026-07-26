using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Analise.Application.Analises.AvaliarComClaude;

public sealed record AvaliarComClaudeCommand(Guid AnaliseId, string? ContextoAdicional) : ICommand;
