using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Fixtures.Application.Equipes.RenomearEquipe;

namespace Oddify.Modules.Fixtures.Presentation.Equipes;

internal sealed class RenomearEquipe : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("equipes/{id}/renomear", async (Guid id, Request request, ISender sender) =>
        {
            Result result = await sender.Send(new RenomearEquipeCommand(id, request.Nome));

            return result.Match(() => Results.Ok(), ApiResults.Problem);
        })
        .WithTags(Tags.Equipes);
    }

    internal sealed class Request
    {
        public string Nome { get; init; }
    }
}
