using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Users.Application.Roles.RemoveRoleFromUser;
using Oddify.Modules.Users.Domain.Permissions;

namespace Oddify.Modules.Users.Presentation.Roles;

internal sealed class RemoveRoleFromUser : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("users/{id}/roles/{roleName}", async (Guid id, string roleName, ISender sender) =>
        {
            Result result = await sender.Send(new RemoveRoleFromUserCommand(id, roleName));

            return result.Match(() => Results.Ok(), ApiResults.Problem);
        })
        .RequireAuthorization(WellKnownPermissions.UsersManageRoles)
        .WithTags(Tags.Users);
    }
}
