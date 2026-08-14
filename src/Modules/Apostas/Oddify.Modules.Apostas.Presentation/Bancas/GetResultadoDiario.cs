using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Apostas.Application.Bancas.GetResultadoDiario;

namespace Oddify.Modules.Apostas.Presentation.Bancas;

internal sealed class GetResultadoDiario : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("bancas/{id}/resultado-diario", async (Guid id, int ano, int mes, ISender sender) =>
        {
            Result<IReadOnlyCollection<ResultadoDiarioResponse>> result = await sender.Send(new GetResultadoDiarioQuery(id, ano, mes));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.Bancas)
        .RequireAuthorization();
    }
}
