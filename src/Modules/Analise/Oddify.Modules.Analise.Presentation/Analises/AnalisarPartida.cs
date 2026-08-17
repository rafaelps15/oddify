using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Analise.Application.Analises.AnalisarPartida;

namespace Oddify.Modules.Analise.Presentation.Analises;

internal sealed class AnalisarPartida : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("partidas/{id}/analisar", async (Guid id, Request request, ISender sender) =>
        {
            Result<Guid> result = await sender.Send(new AnalisarPartidaCommand(id, request.Mercado));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.Analises);
    }

    internal sealed class Request
    {
        public string Mercado { get; init; }
    }
}
