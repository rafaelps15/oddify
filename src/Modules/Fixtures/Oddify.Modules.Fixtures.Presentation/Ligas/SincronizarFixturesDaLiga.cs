using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Fixtures.Application.Ligas.SincronizarFixturesDaLiga;

namespace Oddify.Modules.Fixtures.Presentation.Ligas;

internal sealed class SincronizarFixturesDaLiga : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("ligas/{id}/sincronizar-fixtures", async (Guid id, [FromQuery] int temporada, ISender sender) =>
        {
            Result result = await sender.Send(new SincronizarFixturesDaLigaCommand(id, temporada));

            return result.Match(() => Results.Ok(), ApiResults.Problem);
        })
        .WithTags(Tags.Ligas);
    }
}
