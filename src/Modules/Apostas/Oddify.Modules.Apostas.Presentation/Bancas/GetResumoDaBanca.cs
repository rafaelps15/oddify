using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Apostas.Application.Bancas.GetResumoDaBanca;

namespace Oddify.Modules.Apostas.Presentation.Bancas;

internal sealed class GetResumoDaBanca : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("bancas/{id}/resumo", async (Guid id, ISender sender, int? dias = null) =>
        {
            Result<ResumoDaBancaResponse> result = await sender.Send(new GetResumoDaBancaQuery(id, dias));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.Bancas)
        .RequireAuthorization();
    }
}
