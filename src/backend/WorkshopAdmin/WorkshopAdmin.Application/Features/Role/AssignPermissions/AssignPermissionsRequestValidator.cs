namespace WorkshopAdmin.Application.Features.Role.AssignPermissions;

using FluentValidation;

public sealed class AssignPermissionsRequestValidator : AbstractValidator<AssignPermissionsRequest>
{
    public AssignPermissionsRequestValidator()
    {
        RuleFor(x => x.PermissionIds).NotEmpty();
        RuleForEach(x => x.PermissionIds).NotEqual(Guid.Empty);
    }
}
