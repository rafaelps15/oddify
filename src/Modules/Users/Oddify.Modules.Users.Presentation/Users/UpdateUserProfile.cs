using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Oddify.Common.Domain;
using Oddify.Common.Presentation.Endpoints;
using Oddify.Common.Presentation.Results;
using Oddify.Modules.Users.Application.Users.UpdateUserProfile;

namespace Oddify.Modules.Users.Presentation.Users;

internal sealed class UpdateUserProfile : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("users/{id}/update-profile", async (Guid id, Request request, ISender sender) =>
        {
            Result result = await sender.Send(new UpdateUserProfileCommand(id, request.FirstName, request.LastName));

            return result.Match(() => Results.Ok(), ApiResults.Problem);
        })
        .WithTags(Tags.Users);
    }

    internal sealed class Request
    {
        public string FirstName { get; init; }
        public string LastName { get; init; }
    }
}
