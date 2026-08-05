using Oddify.Modules.Users.Domain.Users;

namespace Oddify.Common.Application.Authentication;

public interface ITokenProvider
{
    string Create(User user);

    string GenerateRefreshToken();
}
