namespace WorkshopAdmin.Application.Features.Tenant.Update;

using FluentValidation;

public sealed class UpdateTenantRequestValidator : AbstractValidator<UpdateTenantRequest>
{
    public UpdateTenantRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);

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
    }
}
