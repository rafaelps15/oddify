namespace Oddify.Common.Application.Authentication;

public interface ITokenProvider
{
    string Create(Guid userId, string email);

    string GenerateRefreshToken();
}
