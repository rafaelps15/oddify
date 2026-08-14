using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Apostas.Application.Bancas.GetDesempenhoPorMercado;

namespace Oddify.Modules.Apostas.Presentation.Bancas;

internal sealed class GetDesempenhoPorMercado : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("bancas/{id}/desempenho/mercados", async (Guid id, ISender sender) =>
        {
            Result<IReadOnlyCollection<DesempenhoResponse>> result = await sender.Send(new GetDesempenhoPorMercadoQuery(id));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.Bancas)
        .RequireAuthorization();
    }
}
