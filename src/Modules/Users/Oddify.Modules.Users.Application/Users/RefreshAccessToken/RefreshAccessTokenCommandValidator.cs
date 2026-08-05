using FluentValidation;

namespace Oddify.Modules.Users.Application.Users.RefreshAccessToken;

internal sealed class RefreshAccessTokenCommandValidator : AbstractValidator<RefreshAccessTokenCommand>
{
    public RefreshAccessTokenCommandValidator()
    {
        RuleFor(c => c.RefreshToken).NotEmpty().WithMessage("O refresh token é obrigatório");
    }
}
