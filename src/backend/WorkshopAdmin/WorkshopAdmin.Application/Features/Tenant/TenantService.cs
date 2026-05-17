namespace WorkshopAdmin.Application.Features.Tenant;

using System.Data;
using System.Data.Common;
using FluentValidation;
using WorkshopAdmin.Application.Common.Codebooks;
using WorkshopAdmin.Application.Common.Interfaces;
using WorkshopAdmin.Application.Common.Models;
using WorkshopAdmin.Application.Features.Auth.Models;
using WorkshopAdmin.Application.Features.Tenant.Create;
using WorkshopAdmin.Application.Features.Tenant.GetById;
using WorkshopAdmin.Application.Features.Tenant.List;
using WorkshopAdmin.Application.Features.Tenant.Models;
using WorkshopAdmin.Application.Features.Tenant.Update;
using WorkshopAdmin.Domain.Exceptions;

public sealed class TenantService(
    IDbConnectionFactory connectionFactory,
    ITenantRepository tenantRepository,
    ISubscriptionPlanRepository subscriptionPlanRepository,
    ICodebookRepository codebookRepository,
    IRoleRepository roleRepository,
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ICurrentUserContext currentUser,
    IEmailSender emailSender,
    IValidator<CreateTenantRequest> createValidator,
    IValidator<UpdateTenantRequest> updateValidator) : ITenantService
{
    private const string TenantAdminRoleName = "tenant_admin";

    // Whitelisted sort columns — never interpolate caller input into ORDER BY.
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["name"] = "name",
        ["created_at"] = "created_at",
        ["is_active"] = "is_active"
    };

    public async Task<PagedResponse<TenantListItem>> ListAsync(ListTenantsRequest request, CancellationToken cancellationToken)
    {
        int limit = request.Limit is > 0 and <= 100 ? request.Limit : 25;
        int offset = request.Offset >= 0 ? request.Offset : 0;

        string sortBy = request.SortBy is not null && SortColumns.TryGetValue(request.SortBy, out string? column)
            ? column
            : "created_at";
        string sortDirection = string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase)
            ? "DESC"
            : "ASC";

        await using DbConnection connection = await OpenConnectionAsync(cancellationToken);

        IReadOnlyList<TenantListItem> items = await tenantRepository.ListAsync(
            request.Search, request.IsActive, offset, limit, sortBy, sortDirection, connection, cancellationToken);
        int total = await tenantRepository.CountAsync(request.Search, request.IsActive, connection, cancellationToken);

        return new PagedResponse<TenantListItem>
        {
            Items = items,
            TotalCount = total,
            Offset = offset,
            Limit = limit
        };
    }

    public async Task<TenantDetailResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await OpenConnectionAsync(cancellationToken);

        return await tenantRepository.GetByIdAsync(id, connection, cancellationToken)
            ?? throw new NotFoundException("Tenant", id);
    }

    public async Task<CreateTenantResponse> CreateAsync(CreateTenantRequest request, CancellationToken cancellationToken)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);

        Guid actingUserId = currentUser.UserId;
        string passwordHash = passwordHasher.Hash(request.Admin.Password);

        await using DbConnection connection = await OpenConnectionAsync(cancellationToken);
        await using DbTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        Guid tenantId;
        Guid adminUserId;
        try
        {
            Guid planId = await subscriptionPlanRepository.ResolveActiveIdByCodeAsync(
                request.SubscriptionPlanCode, connection, transaction, cancellationToken)
                ?? throw new BusinessRuleException($"Subscription plan '{request.SubscriptionPlanCode}' was not found or is inactive.");

            short currencyId = await codebookRepository.ResolveIdByCodeAsync(
                CodebookTable.Currencies, request.CurrencyCode, connection, transaction, cancellationToken)
                ?? throw new BusinessRuleException($"Currency '{request.CurrencyCode}' was not found or is inactive.");

            if (await tenantRepository.SlugExistsAsync(request.Slug, connection, transaction, cancellationToken))
            {
                throw new ConflictException($"A tenant with slug '{request.Slug}' already exists.");
            }

            if (await userRepository.EmailExistsAsync(request.Admin.Email, connection, transaction, cancellationToken))
            {
                throw new ConflictException($"A user with email '{request.Admin.Email}' already exists.");
            }

            Guid tenantAdminRoleId = await roleRepository.GetGlobalIdByNameAsync(
                TenantAdminRoleName, connection, transaction, cancellationToken)
                ?? throw new BusinessRuleException("The global 'tenant_admin' role is missing. Run the database migrations.");

            tenantId = await tenantRepository.InsertAsync(
                new TenantInsert(
                    request.Name, request.Slug, request.ContactEmail, request.ContactPhone,
                    planId, currencyId,
                    request.AddressLine1, request.AddressLine2, request.City, request.PostalCode, request.Country),
                actingUserId, connection, transaction, cancellationToken);

            adminUserId = await userRepository.CreateAsync(
                new NewUser(request.Admin.Email, passwordHash, request.Admin.FirstName, request.Admin.LastName, tenantId),
                actingUserId, connection, transaction, cancellationToken);

            await userRepository.AssignRoleAsync(adminUserId, tenantAdminRoleId, actingUserId, connection, transaction, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        // Best-effort welcome email — must never roll back a committed tenant.
        await TrySendWelcomeEmailAsync(request, cancellationToken);

        return new CreateTenantResponse(tenantId, adminUserId);
    }

    public async Task UpdateAsync(Guid id, UpdateTenantRequest request, CancellationToken cancellationToken)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        Guid actingUserId = currentUser.UserId;

        await using DbConnection connection = await OpenConnectionAsync(cancellationToken);

        Guid planId = await subscriptionPlanRepository.ResolveActiveIdByCodeAsync(
            request.SubscriptionPlanCode, connection, null, cancellationToken)
            ?? throw new BusinessRuleException($"Subscription plan '{request.SubscriptionPlanCode}' was not found or is inactive.");

        short currencyId = await codebookRepository.ResolveIdByCodeAsync(
            CodebookTable.Currencies, request.CurrencyCode, connection, null, cancellationToken)
            ?? throw new BusinessRuleException($"Currency '{request.CurrencyCode}' was not found or is inactive.");

        bool updated = await tenantRepository.UpdateAsync(
            id,
            new TenantUpdate(
                request.Name, request.ContactEmail, request.ContactPhone, planId, currencyId,
                request.AddressLine1, request.AddressLine2, request.City, request.PostalCode, request.Country),
            actingUserId, connection, cancellationToken);

        if (!updated)
        {
            throw new NotFoundException("Tenant", id);
        }
    }

    public async Task SetActivationAsync(Guid id, bool isActive, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await OpenConnectionAsync(cancellationToken);

        bool changed = await tenantRepository.SetActiveAsync(id, isActive, currentUser.UserId, connection, cancellationToken);
        if (!changed)
        {
            throw new NotFoundException("Tenant", id);
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await OpenConnectionAsync(cancellationToken);

        bool deleted = await tenantRepository.SoftDeleteAsync(id, currentUser.UserId, connection, cancellationToken);
        if (!deleted)
        {
            throw new NotFoundException("Tenant", id);
        }
    }

    private async Task TrySendWelcomeEmailAsync(CreateTenantRequest request, CancellationToken cancellationToken)
    {
        try
        {
            string subject = $"Your WorkshopAdmin workspace '{request.Name}' is ready";
            string html =
                $"<p>Hello {request.Admin.FirstName},</p>" +
                $"<p>The workspace <strong>{request.Name}</strong> has been created and your administrator " +
                $"account (<strong>{request.Admin.Email}</strong>) is active.</p>" +
                "<p>You can now sign in with the password provided to you.</p>";

            await emailSender.SendAsync(
                new EmailMessage(request.Admin.Email, subject, html),
                cancellationToken);
        }
        catch
        {
            // Swallowed by design: the tenant is already committed. The email
            // sender implementation is responsible for logging delivery.
        }
    }

    private async Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        IDbConnection connection = connectionFactory.CreateConnection();
        if (connection is not DbConnection dbConnection)
        {
            throw new InvalidOperationException("The configured connection does not support asynchronous operations.");
        }

        await dbConnection.OpenAsync(cancellationToken);
        return dbConnection;
    }
}
