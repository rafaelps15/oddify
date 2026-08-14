using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Apostas.Application.ApostasMultiplas.ExcluirApostaMultipla;

namespace Oddify.Modules.Apostas.Presentation.ApostasMultiplas;

internal sealed class ExcluirApostaMultipla : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("apostas-multiplas/{id}", async (Guid id, ISender sender) =>
        {
            Result result = await sender.Send(new ExcluirApostaMultiplaCommand(id));

            return result.Match(Results.NoContent, ApiResults.Problem);
        })
        .WithTags(Tags.ApostasMultiplas)
        .RequireAuthorization();
    }
}
