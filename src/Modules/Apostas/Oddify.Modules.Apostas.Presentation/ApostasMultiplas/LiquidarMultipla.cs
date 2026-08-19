using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Application.Authentication;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Apostas.Application.ApostasMultiplas.LiquidarMultipla;

namespace Oddify.Modules.Apostas.Presentation.ApostasMultiplas;

internal sealed class LiquidarMultipla : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("apostas-multiplas/{id}/liquidar", async (Guid id, ISender sender, IUserContext userContext) =>
        {
            Result result = await sender.Send(new LiquidarMultiplaCommand(id, userContext.UserId));

            return result.Match(() => Results.Ok(), ApiResults.Problem);
        })
        .WithTags(Tags.ApostasMultiplas)
        .RequireAuthorization();
    }
}
