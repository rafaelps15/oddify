using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Fixtures.Application.Partidas.SincronizarAoVivo;

namespace Oddify.Modules.Fixtures.Presentation.Partidas;

internal sealed class SincronizarAoVivo : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("partidas/sincronizar-ao-vivo", async (ISender sender) =>
        {
            Result result = await sender.Send(new SincronizarAoVivoCommand());

            return result.Match(() => Results.Ok(), ApiResults.Problem);
        })
        .WithTags(Tags.Partidas);
    }
}
