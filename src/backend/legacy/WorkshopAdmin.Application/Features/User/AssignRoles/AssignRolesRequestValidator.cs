namespace WorkshopAdmin.Application.Features.User.AssignRoles;

using FluentValidation;

public sealed class AssignRolesRequestValidator : AbstractValidator<AssignRolesRequest>
{
    public AssignRolesRequestValidator()
    {
        RuleFor(x => x.RoleIds).NotEmpty();
        RuleForEach(x => x.RoleIds).NotEqual(Guid.Empty);
    }
}
