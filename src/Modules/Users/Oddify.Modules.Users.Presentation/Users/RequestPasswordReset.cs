using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Users.Application.Users.RequestPasswordReset;

namespace Oddify.Modules.Users.Presentation.Users;

internal sealed class RequestPasswordReset : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/password-reset/request", async (Request request, ISender sender) =>
        {
            Result result = await sender.Send(new RequestPasswordResetCommand(request.Email));

            return result.Match(Results.NoContent, ApiResults.Problem);
        })
        .WithTags(Tags.Users);
    }

    internal sealed class Request
    {
        public string Email { get; init; }
    }
}
