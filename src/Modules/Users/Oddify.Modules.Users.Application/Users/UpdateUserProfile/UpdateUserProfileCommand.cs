using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Users.Application.Users.UpdateUserProfile;

public sealed record UpdateUserProfileCommand(Guid UserId, string FirstName, string LastName) : ICommand;
