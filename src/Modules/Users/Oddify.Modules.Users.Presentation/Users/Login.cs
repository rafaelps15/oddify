using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Users.Application.Users;
using Oddify.Modules.Users.Application.Users.Login;

namespace Oddify.Modules.Users.Presentation.Users;

internal sealed class Login : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/login", async (Request request, ISender sender) =>
        {
            Result<AccessTokensResponse> result = await sender.Send(new LoginCommand(request.Email, request.Password));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.Users);
    }

    internal sealed class Request
    {
        public string Email { get; init; }
        public string Password { get; init; }
    }
}
