using Microsoft.AspNetCore.Authorization;
using Oddify.Common.Application.Authorization;
using Oddify.Common.Infrastructure.Authentication;

namespace Oddify.Common.Infrastructure.Authorization;

internal sealed class PermissionAuthorizationHandler(IPermissionService permissionService)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        Guid userId = context.User.GetUserId();

        IReadOnlySet<string> permissions = await permissionService.GetPermissionsAsync(userId);

        if (permissions.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
        }
    }
}
