using FluentValidation;

namespace SecureChat.Application.Auth;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("사용자명을 입력하세요.")
            .Length(3, 32).WithMessage("사용자명은 3~32자여야 합니다.")
            .Matches(@"^[a-zA-Z0-9_]+$")
            .WithMessage("사용자명은 영문, 숫자, 밑줄(_)만 사용할 수 있습니다.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("비밀번호를 입력하세요.")
            .MinimumLength(8).WithMessage("비밀번호는 최소 8자 이상이어야 합니다.")
            .MaximumLength(128).WithMessage("비밀번호는 128자를 초과할 수 없습니다.")
            .Must(HasMixedCharClasses)
            .WithMessage("비밀번호는 소문자·대문자·숫자·특수문자 중 2종류 이상을 포함해야 합니다.");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
            .MaximumLength(254);

        RuleFor(x => x.DisplayName)
            .MaximumLength(64).When(x => x.DisplayName is not null);
    }

    private static bool HasMixedCharClasses(string pw)
    {
        if (string.IsNullOrEmpty(pw)) return false;
        int classes =
            (pw.Any(char.IsLower) ? 1 : 0) +
            (pw.Any(char.IsUpper) ? 1 : 0) +
            (pw.Any(char.IsDigit) ? 1 : 0) +
            (pw.Any(c => !char.IsLetterOrDigit(c)) ? 1 : 0);
        return classes >= 2;
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
    }
}
