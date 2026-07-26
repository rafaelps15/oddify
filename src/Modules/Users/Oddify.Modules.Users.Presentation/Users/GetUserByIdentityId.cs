using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Users.Application.Users.GetUser;
using Oddify.Modules.Users.Application.Users.GetUserByIdentityId;

namespace Oddify.Modules.Users.Presentation.Users;

internal sealed class GetUserByIdentityId : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("users/by-identity/{identityId}", async (string identityId, ISender sender) =>
        {
            Result<UserResponse> result = await sender.Send(new GetUserByIdentityIdQuery(identityId));
            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.Users);
    }
}
