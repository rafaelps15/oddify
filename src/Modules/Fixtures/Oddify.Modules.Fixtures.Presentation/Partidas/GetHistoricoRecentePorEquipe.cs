using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Fixtures.Application.Partidas.GetHistoricoRecentePorEquipe;

namespace Oddify.Modules.Fixtures.Presentation.Partidas;

internal sealed class GetHistoricoRecentePorEquipe : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("equipes/{equipeId}/historico-recente", async (Guid equipeId, [FromQuery] int minimoDeJogos, ISender sender) =>
        {
            Result<HistoricoDeEquipeResponse> result = await sender.Send(
                new GetHistoricoRecentePorEquipeQuery(equipeId, minimoDeJogos));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.Partidas);
    }
}
