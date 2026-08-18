using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Fixtures.Application.EstatisticasDeEquipe.GetEstatisticasDeEquipe;

namespace Oddify.Modules.Fixtures.Presentation.EstatisticasDeEquipe;

internal sealed class GetEstatisticasDeEquipe : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("estatisticas-de-equipe", async ([FromQuery] Guid partidaId, ISender sender) =>
        {
            Result<IReadOnlyCollection<EstatisticaEquipeResponse>> result =
                await sender.Send(new GetEstatisticasDeEquipeQuery(partidaId));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.EstatisticasDeEquipe);
    }
}
