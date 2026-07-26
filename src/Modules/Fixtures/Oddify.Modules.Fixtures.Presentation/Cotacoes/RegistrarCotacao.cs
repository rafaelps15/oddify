using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Fixtures.Application.Cotacoes.RegistrarCotacao;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Oddify.Modules.Fixtures.Presentation.Cotacoes;

internal sealed class RegistrarCotacao : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("cotacoes", async (Request request, ISender sender) =>
        {
            Result<Guid> result = await sender.Send(
                new RegistrarCotacaoCommand(request.PartidaId, request.Mercado, request.Odd, request.Casa, request.ColetadaEmUtc));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.Cotacoes);
    }

    internal sealed class Request
    {
        public Guid PartidaId { get; init; }

        public string Mercado { get; init; }

        public decimal Odd { get; init; }

        public string Casa { get; init; }

        public DateTime ColetadaEmUtc { get; init; }
    }
}
