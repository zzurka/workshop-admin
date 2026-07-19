using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using WorkshopAdmin.Modules.Codebook.Contracts;
using WorkshopAdmin.Modules.Codebook.Persistence;
using WorkshopAdmin.SharedKernel.Results;
using WorkshopAdmin.SharedKernel.Validation;

namespace WorkshopAdmin.Modules.Codebook.Features;

/// <summary>PUT /api/codebook/service_types/{id} — dedicated slice (O2).</summary>
internal static class UpdateServiceType
{
    public static void Map(RouteGroupBuilder group) =>
        // TODO(F3): permission "codebook:manage"
        group.MapPut($"/{CodebookTypes.ServiceTypes}/{{id}}", async (
                short id,
                UpdateServiceTypeRequest request,
                UpdateServiceTypeHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(id, request, cancellationToken)).ToHttpResult())
            .WithValidation<UpdateServiceTypeRequest>();
}

internal sealed record UpdateServiceTypeRequest(
    Dictionary<string, string> Label, short? DefaultDurationMin, short SortOrder = 0);

internal sealed class UpdateServiceTypeRequestValidator : AbstractValidator<UpdateServiceTypeRequest>
{
    public UpdateServiceTypeRequestValidator()
    {
        RuleFor(r => r.Label).ApplyCodebookLabelRules();
        RuleFor(r => r.DefaultDurationMin).GreaterThan((short)0).When(r => r.DefaultDurationMin is not null);
    }
}

internal sealed class UpdateServiceTypeHandler(CodebookCache cache, CodebookDbContext db)
{
    public async Task<Result<ServiceTypeResponse>> HandleAsync(
        short id, UpdateServiceTypeRequest request, CancellationToken cancellationToken)
    {
        ServiceType? serviceType = await db.Set<ServiceType>().SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (serviceType is null)
        {
            return Error.NotFound("codebook.entry_not_found", $"No entry {id} in '{CodebookTypes.ServiceTypes}'.");
        }

        serviceType.Label = request.Label;
        serviceType.DefaultDurationMin = request.DefaultDurationMin;
        serviceType.SortOrder = request.SortOrder;

        await db.SaveChangesAsync(cancellationToken);
        cache.Invalidate(CodebookTypes.ServiceTypes);

        return new ServiceTypeResponse(serviceType.Id, serviceType.Code, serviceType.Label,
            serviceType.DefaultDurationMin, serviceType.SortOrder, serviceType.IsActive);
    }
}
