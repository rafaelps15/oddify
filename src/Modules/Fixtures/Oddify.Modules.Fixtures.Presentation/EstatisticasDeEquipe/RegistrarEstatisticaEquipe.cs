using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Fixtures.Application.EstatisticasDeEquipe.RegistrarEstatisticaEquipe;

namespace Oddify.Modules.Fixtures.Presentation.EstatisticasDeEquipe;

internal sealed class RegistrarEstatisticaEquipe : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("estatisticas-de-equipe", async (Request request, ISender sender) =>
        {
            Result<Guid> result = await sender.Send(
                new RegistrarEstatisticaEquipeCommand(
                    request.PartidaId,
                    request.EquipeId,
                    request.Gols,
                    request.Finalizacoes,
                    request.Escanteios,
                    request.Posse));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.EstatisticasDeEquipe);
    }

    internal sealed class Request
    {
        public Guid PartidaId { get; init; }

        public Guid EquipeId { get; init; }

        public int Gols { get; init; }

        public int Finalizacoes { get; init; }

        public int Escanteios { get; init; }

        public decimal Posse { get; init; }
    }
}
