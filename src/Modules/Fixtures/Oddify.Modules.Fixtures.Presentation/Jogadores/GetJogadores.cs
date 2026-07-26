using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Fixtures.Application.Jogadores.GetJogador;
using Oddify.Modules.Fixtures.Application.Jogadores.GetJogadores;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Oddify.Modules.Fixtures.Presentation.Jogadores;

internal sealed class GetJogadores : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("jogadores", async ([FromQuery] Guid equipeId, ISender sender) =>
        {
            Result<IReadOnlyCollection<JogadorResponse>> result = await sender.Send(new GetJogadoresQuery(equipeId));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.Jogadores);
    }
}
