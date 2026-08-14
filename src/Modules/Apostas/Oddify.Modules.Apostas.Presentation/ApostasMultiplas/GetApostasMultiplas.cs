using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Apostas.Application.ApostasMultiplas.GetApostaMultipla;
using Oddify.Modules.Apostas.Application.ApostasMultiplas.GetApostasMultiplas;

namespace Oddify.Modules.Apostas.Presentation.ApostasMultiplas;

internal sealed class GetApostasMultiplas : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("apostas-multiplas", async ([FromQuery] Guid? bancaId, ISender sender) =>
        {
            Result<IReadOnlyCollection<ApostaMultiplaResponse>> result = await sender.Send(new GetApostasMultiplasQuery(bancaId));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.ApostasMultiplas)
        .RequireAuthorization();
    }
}
