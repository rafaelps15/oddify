using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Apostas.Application.Bancas.GetDistribuicaoDeResultados;

namespace Oddify.Modules.Apostas.Presentation.Bancas;

internal sealed class GetDistribuicaoDeResultados : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("bancas/{id}/distribuicao", async (Guid id, ISender sender) =>
        {
            Result<DistribuicaoDeResultadosResponse> result = await sender.Send(new GetDistribuicaoDeResultadosQuery(id));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.Bancas)
        .RequireAuthorization();
    }
}
