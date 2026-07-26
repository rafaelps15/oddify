using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Fixtures.Application.Ligas.CriarLiga;

namespace Oddify.Modules.Fixtures.Presentation.Ligas;

internal sealed class CriarLiga : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("ligas", async (Request request, ISender sender) =>
        {
            Result<Guid> result = await sender.Send(
                new CriarLigaCommand(request.IdExterno, request.Nome, request.MediaDeGols, request.FatorCasa));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.Ligas);
    }

    internal sealed class Request
    {
        public string IdExterno { get; init; }

        public string Nome { get; init; }

        public decimal MediaDeGols { get; init; }

        public decimal FatorCasa { get; init; }
    }
}
