using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Fixtures.Application.Equipes.GetEquipe;
using Oddify.Modules.Fixtures.Application.Equipes.GetEquipes;

namespace Oddify.Modules.Fixtures.Presentation.Equipes;

internal sealed class GetEquipes : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("equipes", async ([FromQuery] Guid ligaId, ISender sender) =>
        {
            Result<IReadOnlyCollection<EquipeResponse>> result = await sender.Send(new GetEquipesQuery(ligaId));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.Equipes);
    }
}
