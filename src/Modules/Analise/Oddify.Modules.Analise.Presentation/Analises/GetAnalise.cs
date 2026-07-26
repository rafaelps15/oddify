using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Analise.Application.Analises.GetAnalise;

namespace Oddify.Modules.Analise.Presentation.Analises;

internal sealed class GetAnalise : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("analises/{id}", async (Guid id, ISender sender) =>
        {
            Result<AnaliseResponse> result = await sender.Send(new GetAnaliseQuery(id));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.Analises);
    }
}
