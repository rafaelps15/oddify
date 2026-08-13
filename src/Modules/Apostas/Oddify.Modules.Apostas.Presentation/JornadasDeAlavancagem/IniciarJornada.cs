using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Apostas.Application.JornadasDeAlavancagem.IniciarJornada;
using Oddify.Modules.Apostas.Domain.JornadasDeAlavancagem;

namespace Oddify.Modules.Apostas.Presentation.JornadasDeAlavancagem;

internal sealed class IniciarJornada : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("jornadas-de-alavancagem", async (Request request, ISender sender) =>
        {
            Result<Guid> result = await sender.Send(new IniciarJornadaCommand(request.FaixaMeta, request.ValorInicial));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.JornadasDeAlavancagem);
    }

    internal sealed class Request
    {
        public FaixaDeMeta FaixaMeta { get; init; }

        public decimal ValorInicial { get; init; }
    }
}
