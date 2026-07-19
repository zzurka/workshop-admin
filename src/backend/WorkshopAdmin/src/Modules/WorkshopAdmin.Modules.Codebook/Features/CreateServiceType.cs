using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using WorkshopAdmin.Modules.Codebook.Contracts;
using WorkshopAdmin.Modules.Codebook.Persistence;
using WorkshopAdmin.SharedKernel.Results;
using WorkshopAdmin.SharedKernel.Validation;

namespace WorkshopAdmin.Modules.Codebook.Features;

/// <summary>POST /api/codebook/service_types — dedicated slice (O2): service types
/// additionally carry a default duration used for day-capacity planning.</summary>
internal static class CreateServiceType
{
    public static void Map(RouteGroupBuilder group) =>
        // TODO(F3): permission "codebook:manage"
        group.MapPost($"/{CodebookTypes.ServiceTypes}", async (
                CreateServiceTypeRequest request,
                CreateServiceTypeHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(request, cancellationToken))
                .ToCreatedResult(serviceType => $"/api/codebook/{CodebookTypes.ServiceTypes}/{serviceType.Id}"))
            .WithValidation<CreateServiceTypeRequest>()
            .WithSummary("Create service type")
            .WithDescription("Adds a service type. The optional default duration (minutes) prefills appointment duration estimates for day-capacity planning.");
}

internal sealed record CreateServiceTypeRequest(
    string Code, Dictionary<string, string> Label, short? DefaultDurationMin, short SortOrder = 0);

internal sealed record ServiceTypeResponse(
    short Id, string Code, Dictionary<string, string> Label, short? DefaultDurationMin, short SortOrder, bool IsActive);

internal sealed class CreateServiceTypeRequestValidator : AbstractValidator<CreateServiceTypeRequest>
{
    public CreateServiceTypeRequestValidator()
    {
        RuleFor(r => r.Code).ApplyCodebookCodeRules();
        RuleFor(r => r.Label).ApplyCodebookLabelRules();
        RuleFor(r => r.DefaultDurationMin).GreaterThan((short)0).When(r => r.DefaultDurationMin is not null);
    }
}

internal sealed class CreateServiceTypeHandler(CodebookCache cache, CodebookDbContext db)
{
    public async Task<Result<ServiceTypeResponse>> HandleAsync(
        CreateServiceTypeRequest request, CancellationToken cancellationToken)
    {
        if (await db.Set<ServiceType>().AnyAsync(s => s.Code == request.Code, cancellationToken))
        {
            return Error.Conflict("codebook.duplicate_code",
                $"Code '{request.Code}' already exists in '{CodebookTypes.ServiceTypes}'.");
        }

        ServiceType serviceType = new()
        {
            Code = request.Code,
            Label = request.Label,
            DefaultDurationMin = request.DefaultDurationMin,
            SortOrder = request.SortOrder
        };

        db.Add(serviceType);
        await db.SaveChangesAsync(cancellationToken);
        cache.Invalidate(CodebookTypes.ServiceTypes);

        return new ServiceTypeResponse(serviceType.Id, serviceType.Code, serviceType.Label,
            serviceType.DefaultDurationMin, serviceType.SortOrder, serviceType.IsActive);
    }
}
