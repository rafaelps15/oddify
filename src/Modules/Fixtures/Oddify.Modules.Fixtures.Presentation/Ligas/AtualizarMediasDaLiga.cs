using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Fixtures.Application.Ligas.AtualizarMediasDaLiga;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Oddify.Modules.Fixtures.Presentation.Ligas;

internal sealed class AtualizarMediasDaLiga : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("ligas/{id}/atualizar-medias", async (Guid id, Request request, ISender sender) =>
        {
            Result result = await sender.Send(new AtualizarMediasDaLigaCommand(id, request.MediaDeGols, request.FatorCasa));

            return result.Match(() => Results.Ok(), ApiResults.Problem);
        })
        .WithTags(Tags.Ligas);
    }

    internal sealed class Request
    {
        public decimal MediaDeGols { get; init; }

        public decimal FatorCasa { get; init; }
    }
}
