namespace WorkshopAdmin.Application.Features.Role.Create;

using FluentValidation;

public sealed class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequest>
{
    public CreateRoleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.Scope)
            .Must(scope => scope is null or "platform" or "tenant")
            .WithMessage("Scope must be 'platform' or 'tenant'.");
    }
}
