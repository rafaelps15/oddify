using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Fixtures.Application.Partidas.RegistrarResultado;

namespace Oddify.Modules.Fixtures.Presentation.Partidas;

internal sealed class RegistrarResultado : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("partidas/{id}/registrar-resultado", async (Guid id, Request request, ISender sender) =>
        {
            Result result = await sender.Send(new RegistrarResultadoCommand(id, request.GolsCasa, request.GolsVisitante));

            return result.Match(() => Results.Ok(), ApiResults.Problem);
        })
        .WithTags(Tags.Partidas);
    }

    internal sealed class Request
    {
        public int GolsCasa { get; init; }

        public int GolsVisitante { get; init; }
    }
}
