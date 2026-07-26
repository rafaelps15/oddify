using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Fixtures.Application.Jogadores.GetJogador;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Oddify.Modules.Fixtures.Presentation.Jogadores;

internal sealed class GetJogador : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("jogadores/{id}", async (Guid id, ISender sender) =>
        {
            Result<JogadorResponse> result = await sender.Send(new GetJogadorQuery(id));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.Jogadores);
    }
}
