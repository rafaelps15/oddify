using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Fixtures.Application.EstatisticasDeJogador.GetEstatisticasDeJogador;

namespace Oddify.Modules.Fixtures.Presentation.EstatisticasDeJogador;

internal sealed class GetEstatisticasDeJogador : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("estatisticas-de-jogador", async ([FromQuery] Guid partidaId, ISender sender) =>
        {
            Result<IReadOnlyCollection<EstatisticaJogadorResponse>> result =
                await sender.Send(new GetEstatisticasDeJogadorQuery(partidaId));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.EstatisticasDeJogador);
    }
}
