using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Apostas.Application.JornadasDeAlavancagem.GetFaixasDeMeta;

namespace Oddify.Modules.Apostas.Presentation.JornadasDeAlavancagem;

internal sealed class GetFaixasDeMeta : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("jornadas-de-alavancagem/faixas", async (ISender sender) =>
        {
            Result<IReadOnlyCollection<FaixaDeMetaResponse>> result = await sender.Send(new GetFaixasDeMetaQuery());

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.JornadasDeAlavancagem)
        .RequireAuthorization();
    }
}
