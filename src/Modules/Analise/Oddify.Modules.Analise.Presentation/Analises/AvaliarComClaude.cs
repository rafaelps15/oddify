using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Analise.Application.Analises.AvaliarComClaude;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Oddify.Modules.Analise.Presentation.Analises;

internal sealed class AvaliarComClaude : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("analises/{id}/avaliar-com-claude", async (Guid id, Request request, ISender sender) =>
        {
            Result result = await sender.Send(new AvaliarComClaudeCommand(id, request.ContextoAdicional));

            return result.Match(() => Results.Ok(), ApiResults.Problem);
        })
        .WithTags(Tags.Analises);
    }

    internal sealed class Request
    {
        public string? ContextoAdicional { get; init; }
    }
}
