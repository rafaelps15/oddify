using FluentValidation;

namespace Oddify.Modules.Users.Application.Users.UpdateUserProfile;

internal sealed class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty().WithMessage("O identificador do usuário é obrigatório");
        RuleFor(c => c.FirstName).NotEmpty().WithMessage("O nome é obrigatório")
            .MaximumLength(200).WithMessage("O nome deve ter no máximo 200 caracteres");
        RuleFor(c => c.LastName).NotEmpty().WithMessage("O sobrenome é obrigatório")
            .MaximumLength(200).WithMessage("O sobrenome deve ter no máximo 200 caracteres");
    }
}
