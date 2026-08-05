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
        app.MapGet("partidas", async ([FromQuery] Guid? ligaId, [FromQuery] StatusFiltroDePartida status, [FromQuery] int? rodada, [FromQuery] int? temporada, ISender sender) =>
            {
                Result<IReadOnlyCollection<PartidaResponse>> result = await sender.Send(new GetPartidasQuery(ligaId, status, rodada, temporada));

                return result.Match(Results.Ok, ApiResults.Problem);
            })
        .WithTags(Tags.Partidas);
    }
}
