using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Apostas.Application.Bancas.GetSugestaoDeStake;

namespace Oddify.Modules.Apostas.Presentation.Bancas;

internal sealed class GetSugestaoDeStake : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "bancas/sugestao-de-stake",
            async (
                [FromQuery] decimal saldoDaBanca,
                [FromQuery] decimal odd,
                [FromQuery] decimal probabilidade,
                [FromQuery] decimal? fracaoDeKelly,
                ISender sender) =>
            {
                Result<SugestaoDeStakeResponse> result =
                    await sender.Send(new GetSugestaoDeStakeQuery(saldoDaBanca, odd, probabilidade, fracaoDeKelly));

                return result.Match(Results.Ok, ApiResults.Problem);
            })
        .WithTags(Tags.Bancas);
    }
}
