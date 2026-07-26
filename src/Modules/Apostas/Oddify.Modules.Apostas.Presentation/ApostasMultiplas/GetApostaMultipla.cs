using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Apostas.Application.ApostasMultiplas.GetApostaMultipla;

namespace Oddify.Modules.Apostas.Presentation.ApostasMultiplas;

internal sealed class GetApostaMultipla : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("apostas-multiplas/{id}", async (Guid id, ISender sender) =>
        {
            Result<ApostaMultiplaResponse> result = await sender.Send(new GetApostaMultiplaQuery(id));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.ApostasMultiplas);
    }
}
