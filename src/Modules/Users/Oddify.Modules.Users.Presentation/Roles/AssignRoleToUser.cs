using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Users.Application.Roles.AssignRoleToUser;
using Oddify.Modules.Users.Domain.Permissions;

namespace Oddify.Modules.Users.Presentation.Roles;

internal sealed class AssignRoleToUser : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/{id}/roles", async (Guid id, Request request, ISender sender) =>
        {
            Result result = await sender.Send(new AssignRoleToUserCommand(id, request.RoleName));

            return result.Match(() => Results.Ok(), ApiResults.Problem);
        })
        .RequireAuthorization(WellKnownPermissions.UsersManageRoles)
        .WithTags(Tags.Users);
    }

    internal sealed class Request
    {
        public string RoleName { get; init; }
    }
}
