using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Apostas.Application.JornadasDeAlavancagem.GetJornada;

namespace Oddify.Modules.Apostas.Presentation.JornadasDeAlavancagem;

internal sealed class GetJornada : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("jornadas-de-alavancagem/{id}", async (Guid id, ISender sender) =>
        {
            Result<JornadaResponse> result = await sender.Send(new GetJornadaQuery(id));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.JornadasDeAlavancagem)
        .RequireAuthorization();
    }
}
