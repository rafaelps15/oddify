using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Fixtures.Application.EscalacoesDeJogador.RegistrarEscalacaoJogador;

namespace Oddify.Modules.Fixtures.Presentation.EscalacoesDeJogador;

internal sealed class RegistrarEscalacaoJogador : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("escalacoes-de-jogador", async (Request request, ISender sender) =>
        {
            Result<Guid> result = await sender.Send(
                new RegistrarEscalacaoJogadorCommand(
                    request.EscalacaoId,
                    request.JogadorId,
                    request.Titular,
                    request.Posicao,
                    request.Numero));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.EscalacoesDeJogador);
    }

    internal sealed class Request
    {
        public Guid EscalacaoId { get; init; }

        public Guid JogadorId { get; init; }

        public bool Titular { get; init; }

        public string Posicao { get; init; }

        public int? Numero { get; init; }
    }
}
