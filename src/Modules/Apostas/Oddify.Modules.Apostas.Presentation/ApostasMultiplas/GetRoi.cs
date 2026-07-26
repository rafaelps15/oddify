using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Apostas.Application.ApostasMultiplas.GetRoi;

namespace Oddify.Modules.Apostas.Presentation.ApostasMultiplas;

internal sealed class GetRoi : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("apostas-multiplas/roi", async ([FromQuery] Guid? bancaId, ISender sender) =>
        {
            Result<RoiResponse> result = await sender.Send(new GetRoiQuery(bancaId));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.ApostasMultiplas);
    }
}
