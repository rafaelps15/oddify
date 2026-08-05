using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Users.Application.Users.ResendVerification;
using Oddify.Modules.Users.Domain.Permissions;

namespace Oddify.Modules.Users.Presentation.Users;

internal sealed class ResendVerification : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/resend-verification", async (ISender sender) =>
        {
            Result result = await sender.Send(new ResendVerificationCommand());

            return result.Match(Results.NoContent, ApiResults.Problem);
        })
        .RequireAuthorization(WellKnownPermissions.UsersUpdate)
        .WithTags(Tags.Users);
    }
}
