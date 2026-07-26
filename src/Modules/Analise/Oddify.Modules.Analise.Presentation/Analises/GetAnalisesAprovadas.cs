using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Analise.Application.Analises.GetAnalise;
using Oddify.Modules.Analise.Application.Analises.GetAnalisesAprovadas;

namespace Oddify.Modules.Analise.Presentation.Analises;

internal sealed class GetAnalisesAprovadas : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("analises/aprovadas", async (ISender sender) =>
        {
            Result<IReadOnlyCollection<AnaliseResponse>> result = await sender.Send(new GetAnalisesAprovadasQuery());

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.Analises);
    }
}
