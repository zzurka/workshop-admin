namespace WorkshopAdmin.Application.Features.Auth.External;

using FluentValidation;

public sealed class ExternalExchangeRequestValidator : AbstractValidator<ExternalExchangeRequest>
{
    public ExternalExchangeRequestValidator()
    {
        RuleFor(x => x.HandoffCode).NotEmpty();
    }
}
