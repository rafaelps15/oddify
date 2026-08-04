using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Fixtures.Application.Partidas.GetRodadasDisponiveis;

namespace Oddify.Modules.Fixtures.Presentation.Partidas;

internal sealed class GetRodadasDisponiveis : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "partidas/rodadas-disponiveis",
            async ([FromQuery] Guid? ligaId, [FromQuery] int temporada, ISender sender) =>
            {
                Result<IReadOnlyCollection<int>> result = await sender.Send(new GetRodadasDisponiveisQuery(ligaId, temporada));

                return result.Match(Results.Ok, ApiResults.Problem);
            })
        .WithTags(Tags.Partidas);
    }
}
