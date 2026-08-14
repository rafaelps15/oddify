using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Apostas.Application.Bancas.GetDesempenhoPorCampeonato;
using Oddify.Modules.Apostas.Application.Bancas.GetDesempenhoPorMercado;

namespace Oddify.Modules.Apostas.Presentation.Bancas;

internal sealed class GetDesempenhoPorCampeonato : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("bancas/{id}/desempenho/campeonatos", async (Guid id, ISender sender) =>
        {
            Result<IReadOnlyCollection<DesempenhoResponse>> result = await sender.Send(new GetDesempenhoPorCampeonatoQuery(id));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.Bancas)
        .RequireAuthorization();
    }
}
