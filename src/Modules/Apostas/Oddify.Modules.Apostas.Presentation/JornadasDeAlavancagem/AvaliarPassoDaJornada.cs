using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Apostas.Application.JornadasDeAlavancagem.AvaliarPassoDaJornada;

namespace Oddify.Modules.Apostas.Presentation.JornadasDeAlavancagem;

// Normalmente disparado sozinho quando a última aposta do passo liquida (ver
// ApostaMultiplaLiquidadaDomainEventHandler) — este endpoint existe pra reavaliar um passo preso
// manualmente; o handler é idempotente (só age se o passo ainda estiver EmAberto).
internal sealed class AvaliarPassoDaJornada : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("jornadas-de-alavancagem/passos/{id}/avaliar", async (Guid id, ISender sender) =>
        {
            Result result = await sender.Send(new AvaliarPassoDaJornadaCommand(id));

            return result.Match(Results.NoContent, ApiResults.Problem);
        })
        .WithTags(Tags.JornadasDeAlavancagem);
    }
}
