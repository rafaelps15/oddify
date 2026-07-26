using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Fixtures.Application.EstatisticasDeJogador.RegistrarEstatisticaJogador;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Oddify.Modules.Fixtures.Presentation.EstatisticasDeJogador;

internal sealed class RegistrarEstatisticaJogador : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("estatisticas-de-jogador", async (Request request, ISender sender) =>
        {
            Result<Guid> result = await sender.Send(
                new RegistrarEstatisticaJogadorCommand(
                    request.PartidaId,
                    request.JogadorId,
                    request.Gols,
                    request.Assistencias,
                    request.Minutos,
                    request.Titular,
                    request.Nota));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.EstatisticasDeJogador);
    }

    internal sealed class Request
    {
        public Guid PartidaId { get; init; }

        public Guid JogadorId { get; init; }

        public int Gols { get; init; }

        public int Assistencias { get; init; }

        public int Minutos { get; init; }

        public bool Titular { get; init; }

        public decimal Nota { get; init; }
    }
}
