using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Users.Application.Roles.GetUserRoles;
using Oddify.Modules.Users.Domain.Permissions;

namespace Oddify.Modules.Users.Presentation.Roles;

internal sealed class GetUserRoles : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("users/{id}/roles", async (Guid id, ISender sender) =>
        {
            Result<IReadOnlyCollection<RoleResponse>> result = await sender.Send(new GetUserRolesQuery(id));
            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .RequireAuthorization(WellKnownPermissions.UsersRead)
        .WithTags(Tags.Users);
    }
}
