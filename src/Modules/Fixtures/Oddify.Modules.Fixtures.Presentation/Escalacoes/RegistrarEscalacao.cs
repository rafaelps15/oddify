using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Fixtures.Application.Escalacoes.RegistrarEscalacao;

namespace Oddify.Modules.Fixtures.Presentation.Escalacoes;

internal sealed class RegistrarEscalacao : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("escalacoes", async (Request request, ISender sender) =>
        {
            Result<Guid> result = await sender.Send(
                new RegistrarEscalacaoCommand(request.PartidaId, request.EquipeId, request.Formacao, request.Tecnico));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.Escalacoes);
    }

    internal sealed class Request
    {
        public Guid PartidaId { get; init; }

        public Guid EquipeId { get; init; }

        public string Formacao { get; init; }

        public string Tecnico { get; init; }
    }
}
