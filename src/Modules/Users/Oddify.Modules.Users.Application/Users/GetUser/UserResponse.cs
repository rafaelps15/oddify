namespace Oddify.Modules.Users.Application.Users.GetUser;

public sealed record UserResponse(Guid Id, string IdentityId, string Email, string FirstName, string LastName);
