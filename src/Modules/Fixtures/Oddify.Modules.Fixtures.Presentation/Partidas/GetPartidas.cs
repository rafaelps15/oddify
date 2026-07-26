using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Fixtures.Application.Partidas.GetPartida;
using Oddify.Modules.Fixtures.Application.Partidas.GetPartidas;

namespace Oddify.Modules.Fixtures.Presentation.Partidas;

internal sealed class GetPartidas : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("partidas", async ([FromQuery] Guid? ligaId, ISender sender) =>
        {
            Result<IReadOnlyCollection<PartidaResponse>> result = await sender.Send(new GetPartidasQuery(ligaId));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.Partidas);
    }
}
