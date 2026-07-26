using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Fixtures.Application.Ligas.GetLiga;
using Oddify.Modules.Fixtures.Application.Ligas.GetLigas;

namespace Oddify.Modules.Fixtures.Presentation.Ligas;

internal sealed class GetLigas : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("ligas", async (ISender sender) =>
        {
            Result<IReadOnlyCollection<LigaResponse>> result = await sender.Send(new GetLigasQuery());

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.Ligas);
    }
}
