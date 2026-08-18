using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Fixtures.Application.Partidas.GetConfrontosDiretos;
using Oddify.Modules.Fixtures.Application.Partidas.GetPartida;

namespace Oddify.Modules.Fixtures.Presentation.Partidas;

internal sealed class GetConfrontosDiretos : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "partidas/confrontos-diretos",
            async ([FromQuery] Guid equipeAId, [FromQuery] Guid equipeBId, ISender sender, [FromQuery] int quantidade = 5) =>
            {
                Result<IReadOnlyCollection<PartidaResponse>> result =
                    await sender.Send(new GetConfrontosDiretosQuery(equipeAId, equipeBId, quantidade));

                return result.Match(Results.Ok, ApiResults.Problem);
            })
        .WithTags(Tags.Partidas);
    }
}
