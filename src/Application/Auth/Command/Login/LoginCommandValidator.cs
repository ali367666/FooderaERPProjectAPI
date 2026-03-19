using FluentValidation;

namespace Application.Auth.Commands.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Request.EmailOrUserName)
            .NotEmpty().WithMessage("Email or username is required")
            .MaximumLength(100);

        RuleFor(x => x.Request.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(6);
    }
}