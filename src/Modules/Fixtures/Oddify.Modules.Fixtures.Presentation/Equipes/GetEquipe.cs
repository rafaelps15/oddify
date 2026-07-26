using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Fixtures.Application.Equipes.GetEquipe;

namespace Oddify.Modules.Fixtures.Presentation.Equipes;

internal sealed class GetEquipe : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("equipes/{id}", async (Guid id, ISender sender) =>
        {
            Result<EquipeResponse> result = await sender.Send(new GetEquipeQuery(id));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.Equipes);
    }
}
