using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Users.Application.Users.ResetPassword;

namespace Oddify.Modules.Users.Presentation.Users;

internal sealed class ResetPassword : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/password-reset/confirm", async (Request request, ISender sender) =>
        {
            Result result = await sender.Send(new ResetPasswordCommand(request.Token, request.NewPassword));

            return result.Match(Results.NoContent, ApiResults.Problem);
        })
        .WithTags(Tags.Users);
    }

    internal sealed class Request
    {
        public string Token { get; init; }
        public string NewPassword { get; init; }
    }
}
