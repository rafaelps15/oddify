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
        app.MapPost("analises/analisar", async (Request request, ISender sender) =>
        {
            Result<Guid> result = await sender.Send(new AnalisarPartidaCommand(request.PartidaId, request.Mercado));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.Analises);
    }

    internal sealed class Request
    {
        public Guid PartidaId { get; init; }

        public string Mercado { get; init; }
    }
}
