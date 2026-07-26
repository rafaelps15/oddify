using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Fixtures.Application.Jogadores.TransferirJogador;

namespace Oddify.Modules.Fixtures.Presentation.Jogadores;

internal sealed class TransferirJogador : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("jogadores/{id}/transferir", async (Guid id, Request request, ISender sender) =>
        {
            Result result = await sender.Send(new TransferirJogadorCommand(id, request.NovaEquipeId));

            return result.Match(() => Results.Ok(), ApiResults.Problem);
        })
        .WithTags(Tags.Jogadores);
    }

    internal sealed class Request
    {
        public Guid NovaEquipeId { get; init; }
    }
}
