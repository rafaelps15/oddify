using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Fixtures.Application.Equipes.CriarEquipe;

namespace Oddify.Modules.Fixtures.Presentation.Equipes;

internal sealed class CriarEquipe : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("equipes", async (Request request, ISender sender) =>
        {
            Result<Guid> result = await sender.Send(new CriarEquipeCommand(request.IdExterno, request.Nome, request.LigaId));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.Equipes);
    }

    internal sealed class Request
    {
        public string IdExterno { get; init; }

        public string Nome { get; init; }

        public Guid LigaId { get; init; }
    }
}
