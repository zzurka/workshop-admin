namespace WorkshopAdmin.Application.Features.User.Create;

using FluentValidation;

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(255).EmailAddress();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneNumber).MaximumLength(50);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(12).MaximumLength(256);

        When(x => x.RoleIds is not null, () =>
        {
            RuleForEach(x => x.RoleIds).NotEqual(Guid.Empty);
        });
    }
}
