using FatSecret.Domain.Models.DTO;
using FluentValidation;

namespace FatSecret.Validation;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequestsDTO>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email обязателен для заполнения")
            .EmailAddress().WithMessage("Некорректный формат email")
            .MaximumLength(255).WithMessage("Email не может превышать 255 символов");

        RuleFor(x => x.Login)
            .NotEmpty().WithMessage("Логин обязателен для заполнения")
            .MinimumLength(3).WithMessage("Логин должен содержать минимум 3 символа")
            .MaximumLength(50).WithMessage("Логин не может превышать 50 символов")
            .Matches("^[a-zA-Z0-9_-]+$").WithMessage("Логин может содержать только буквы, цифры, знаки подчеркивания и дефисы");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Пароль обязателен для заполнения")
            .MinimumLength(8).WithMessage("Пароль должен содержать минимум 8 символов")
            .MaximumLength(100).WithMessage("Пароль не может превышать 100 символов")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)").WithMessage("Пароль должен содержать минимум одну строчную букву, одну заглавную букву и одну цифру");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Имя обязательно для заполнения")
            .MinimumLength(2).WithMessage("Имя должно содержать минимум 2 символа")
            .MaximumLength(50).WithMessage("Имя не может превышать 50 символов")
            .Matches("^[a-zA-Zа-яА-ЯёЁ\\s-]+$").WithMessage("Имя может содержать только буквы, пробелы и дефисы");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Фамилия обязательна для заполнения")
            .MinimumLength(2).WithMessage("Фамилия должна содержать минимум 2 символа")
            .MaximumLength(50).WithMessage("Фамилия не может превышать 50 символов")
            .Matches("^[a-zA-Zа-яА-ЯёЁ\\s-]+$").WithMessage("Фамилия может содержать только буквы, пробелы и дефисы");
    }
}