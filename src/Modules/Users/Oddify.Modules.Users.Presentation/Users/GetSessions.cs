using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Users.Application.Users.GetSessions;
using Oddify.Modules.Users.Domain.Permissions;

namespace Oddify.Modules.Users.Presentation.Users;

internal sealed class GetSessions : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("users/sessions", async (ISender sender) =>
        {
            Result<IReadOnlyCollection<SessionResponse>> result = await sender.Send(new GetSessionsQuery());

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .RequireAuthorization(WellKnownPermissions.UsersUpdate)
        .WithTags(Tags.Users);
    }
}
