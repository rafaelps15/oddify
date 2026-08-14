using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Apostas.Application.JornadasDeAlavancagem.MontarPassoDaJornada;

namespace Oddify.Modules.Apostas.Presentation.JornadasDeAlavancagem;

internal sealed class MontarPassoDaJornada : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("jornadas-de-alavancagem/{id}/passos", async (Guid id, ISender sender) =>
        {
            Result<Guid> result = await sender.Send(new MontarPassoDaJornadaCommand(id));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.JornadasDeAlavancagem)
        .RequireAuthorization();
    }
}
