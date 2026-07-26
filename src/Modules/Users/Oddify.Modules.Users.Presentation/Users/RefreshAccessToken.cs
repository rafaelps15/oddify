using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Users.Application.Users;
using Oddify.Modules.Users.Application.Users.RefreshAccessToken;

namespace Oddify.Modules.Users.Presentation.Users;

internal sealed class RefreshAccessToken : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/refresh", async (Request request, ISender sender) =>
        {
            Result<AccessTokensResponse> result = await sender.Send(new RefreshAccessTokenCommand(request.RefreshToken));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.Users);
    }

    internal sealed class Request
    {
        public string RefreshToken { get; init; }
    }
}
