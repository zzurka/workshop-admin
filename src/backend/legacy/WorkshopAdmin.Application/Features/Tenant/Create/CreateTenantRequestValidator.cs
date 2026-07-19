namespace WorkshopAdmin.Application.Features.Tenant.Create;

using FluentValidation;

public sealed class CreateTenantRequestValidator : AbstractValidator<CreateTenantRequest>
{
    public CreateTenantRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);

        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(100)
            .Matches("^[a-z0-9]+(-[a-z0-9]+)*$")
            .WithMessage("Slug must be lowercase alphanumeric words separated by single hyphens.");

        RuleFor(x => x.ContactEmail).MaximumLength(255).EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));
        RuleFor(x => x.ContactPhone).MaximumLength(50);

        RuleFor(x => x.SubscriptionPlanCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.CurrencyCode).NotEmpty().MaximumLength(50);

        RuleFor(x => x.AddressLine1).MaximumLength(255);
        RuleFor(x => x.AddressLine2).MaximumLength(255);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.PostalCode).MaximumLength(20);
        RuleFor(x => x.Country).MaximumLength(100);

        RuleFor(x => x.Admin).NotNull().SetValidator(new InitialAdminValidator());
    }
}

public sealed class InitialAdminValidator : AbstractValidator<InitialAdmin>
{
    public InitialAdminValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(255).EmailAddress();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(12).MaximumLength(256);
    }
}
