using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Fixtures.Application.Cotacoes.GetCotacoesPorPartida;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Oddify.Modules.Fixtures.Presentation.Cotacoes;

internal sealed class GetCotacoesPorPartida : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("partidas/{partidaId}/cotacoes", async (Guid partidaId, ISender sender) =>
        {
            Result<IReadOnlyCollection<CotacaoResponse>> result = await sender.Send(new GetCotacoesPorPartidaQuery(partidaId));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.Cotacoes);
    }
}
