using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Apostas.Application.Bancas.GetPerfilDoApostador;

namespace Oddify.Modules.Apostas.Presentation.Bancas;

internal sealed class GetPerfilDoApostador : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("bancas/{id}/perfil-do-apostador", async (Guid id, ISender sender) =>
        {
            Result<PerfilDoApostadorResponse> result = await sender.Send(new GetPerfilDoApostadorQuery(id));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.Bancas);
    }
}
