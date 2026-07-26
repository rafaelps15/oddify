using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using Oddify.Common.Domain;
using Oddify.Modules.Analise.Application.Abstractions.Llm;
using Oddify.Modules.Analise.Domain.Analises;

namespace Oddify.Modules.Analise.Infrastructure.Llm;

internal sealed class ClaudeAvaliadorCriticoService(AnthropicClient client) : IClaudeAvaliadorCriticoService
{
    private const string SystemPrompt =
        """
        Você é o avaliador crítico de um sistema quantitativo de apostas esportivas.
        Uma oportunidade estatística foi identificada por um modelo de Poisson com correção
        de Dixon-Coles e já passou por um filtro de vantagem mínima contra a odd de mercado.
        Sua função é ser cético por padrão: procure ativamente razões para duvidar da aposta
        antes que ela seja confirmada, considerando o contexto adicional fornecido (desfalques,
        lesões, calendário, mudança de técnico, relevância do jogo, etc., quando houver).

        Decida entre três opções:
        - CONFIRMA: a vantagem estatística parece robusta; nada no contexto a contradiz.
        - REDUZ: a vantagem existe, mas há um fator de risco que justifica reduzir a exposição.
        - VETA: há um fator que invalida a vantagem estatística.

        Responda apenas com o JSON estruturado solicitado, em português.
        """;

    private static readonly string[] DecisoesValidas = ["CONFIRMA", "REDUZ", "VETA"];
    private static readonly string[] CamposObrigatorios = ["decisao", "justificativa"];

    public async Task<Result<VeredictoClaude>> AvaliarAsync(AnaliseContexto contexto, CancellationToken cancellationToken = default)
    {
        string userMessage =
            $"""
             Partida: {contexto.PartidaId}
             Mercado: {contexto.Mercado}
             Probabilidade (Poisson puro): {contexto.ProbPoissonPura:P2}
             Probabilidade (Dixon-Coles): {contexto.ProbDixonColes:P2}
             Probabilidade implícita da odd: {contexto.ProbImplicitaDaOdd:P2}
             Vantagem estimada: {contexto.Vantagem:P2}
             Odd de mercado: {contexto.OddDeMercado}
             Contexto adicional: {contexto.ContextoAdicional ?? "nenhum informado"}
             """;

        try
        {
            Message response = await client.Messages.Create(new MessageCreateParams
            {
                Model = Model.ClaudeOpus4_8,
                MaxTokens = 4096,
                System = SystemPrompt,
                Thinking = new ThinkingConfigAdaptive(),
                OutputConfig = new OutputConfig
                {
                    Format = new JsonOutputFormat
                    {
                        Schema = new Dictionary<string, JsonElement>
                        {
                            ["type"] = JsonSerializer.SerializeToElement("object"),
                            ["properties"] = JsonSerializer.SerializeToElement(new
                            {
                                decisao = new { type = "string", @enum = DecisoesValidas },
                                justificativa = new { type = "string" },
                            }),
                            ["required"] = JsonSerializer.SerializeToElement(CamposObrigatorios),
                            ["additionalProperties"] = JsonSerializer.SerializeToElement(false),
                        },
                    },
                },
                Messages = [new() { Role = Role.User, Content = userMessage }],
            }, cancellationToken);

            if (response.StopReason == "refusal")
            {
                return Result.Failure<VeredictoClaude>(Error.Problem("Analises.ClaudeRecusou", "O Claude recusou avaliar esta análise"));
            }

            string? textoJson = response.Content
                .Select(bloco => bloco.Value)
                .OfType<TextBlock>()
                .Select(bloco => bloco.Text)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(textoJson))
            {
                return Result.Failure<VeredictoClaude>(Error.Failure("Analises.RespostaVazia", "O Claude não retornou conteúdo de texto"));
            }

            VeredictoJsonDto? veredictoJson = JsonSerializer.Deserialize<VeredictoJsonDto>(textoJson);

            if (veredictoJson is null)
            {
                return Result.Failure<VeredictoClaude>(Error.Failure("Analises.RespostaInvalida", "Não foi possível interpretar a resposta do Claude"));
            }

            DecisaoDoClaude decisao = veredictoJson.Decisao.ToUpperInvariant() switch
            {
                "CONFIRMA" => DecisaoDoClaude.Confirma,
                "REDUZ" => DecisaoDoClaude.Reduz,
                "VETA" => DecisaoDoClaude.Veta,
                _ => DecisaoDoClaude.Veta,
            };

            return new VeredictoClaude(decisao, veredictoJson.Justificativa, textoJson);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Result.Failure<VeredictoClaude>(Error.Failure("Analises.ErroNaChamadaAoClaude", ex.Message));
        }
    }

    private sealed record VeredictoJsonDto(string Decisao, string Justificativa);
}
