using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Fixtures.Application.Jogadores.CriarJogador;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Oddify.Modules.Fixtures.Presentation.Jogadores;

internal sealed class CriarJogador : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("jogadores", async (Request request, ISender sender) =>
        {
            Result<Guid> result = await sender.Send(
                new CriarJogadorCommand(request.IdExterno, request.EquipeId, request.Nome, request.Posicao));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.Jogadores);
    }

    internal sealed class Request
    {
        public string IdExterno { get; init; }

        public Guid EquipeId { get; init; }

        public string Nome { get; init; }

        public string Posicao { get; init; }
    }
}
