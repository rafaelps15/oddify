using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Apostas.Application.Bancas.GetBanca;
using Oddify.Modules.Apostas.Application.Bancas.GetBancas;

namespace Oddify.Modules.Apostas.Presentation.Bancas;

internal sealed class GetBancas : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("bancas", async (ISender sender) =>
        {
            Result<IReadOnlyCollection<BancaResponse>> result = await sender.Send(new GetBancasQuery());

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.Bancas);
    }
}
