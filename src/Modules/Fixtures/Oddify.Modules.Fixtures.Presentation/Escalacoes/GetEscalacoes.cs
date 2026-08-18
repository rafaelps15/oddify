using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Fixtures.Application.Escalacoes.GetEscalacoes;

namespace Oddify.Modules.Fixtures.Presentation.Escalacoes;

internal sealed class GetEscalacoes : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("escalacoes", async ([FromQuery] Guid partidaId, ISender sender) =>
        {
            Result<IReadOnlyCollection<EscalacaoResponse>> result = await sender.Send(new GetEscalacoesQuery(partidaId));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.Escalacoes);
    }
}
