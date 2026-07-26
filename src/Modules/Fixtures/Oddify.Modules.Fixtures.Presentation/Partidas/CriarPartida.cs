using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Fixtures.Application.Partidas.CriarPartida;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Oddify.Modules.Fixtures.Presentation.Partidas;

internal sealed class CriarPartida : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("partidas", async (Request request, ISender sender) =>
        {
            Result<Guid> result = await sender.Send(
                new CriarPartidaCommand(
                    request.IdExterno,
                    request.LigaId,
                    request.EquipeCasaId,
                    request.EquipeVisitanteId,
                    request.DataUtc));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.Partidas);
    }

    internal sealed class Request
    {
        public string IdExterno { get; init; }

        public Guid LigaId { get; init; }

        public Guid EquipeCasaId { get; init; }

        public Guid EquipeVisitanteId { get; init; }

        public DateTime DataUtc { get; init; }
    }
}
