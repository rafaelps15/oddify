using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Fixtures.Application.Partidas.GetRodadaMaisRecenteEncerrada;

namespace Oddify.Modules.Fixtures.Presentation.Partidas;

internal sealed class GetRodadaMaisRecenteEncerrada : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "partidas/rodada-mais-recente-encerrada",
            async ([FromQuery] Guid? ligaId, [FromQuery] int temporada, ISender sender) =>
            {
                Result<int?> result = await sender.Send(new GetRodadaMaisRecenteEncerradaQuery(ligaId, temporada));

                return result.Match(Results.Ok, ApiResults.Problem);
            })
        .WithTags(Tags.Partidas);
    }
}
