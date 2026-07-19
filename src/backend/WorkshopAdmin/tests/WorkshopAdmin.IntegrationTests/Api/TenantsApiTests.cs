using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using WorkshopAdmin.IntegrationTests.Postgres;
using Xunit;

namespace WorkshopAdmin.IntegrationTests.Api;

[Collection(DatabaseCollection.Name)]
public sealed class TenantsApiTests(PostgresFixture fixture) : IDisposable
{
    private readonly ApiFactory _factory = new(fixture);

    public void Dispose() => _factory.Dispose();

    private sealed record PlanDto(
        Guid Id, string Code, Dictionary<string, string> Label, decimal Price, short CurrencyId,
        short BillingPeriodId, short TrialDays, JsonElement Features, bool IsPublic, bool IsActive);

    private sealed record PlanRefDto(Guid Id, string Code, Dictionary<string, string> Label);

    private sealed record TenantDto(
        Guid Id, string Name, string Slug, string? ContactEmail, short DefaultCurrencyId, string? TaxId,
        bool IsVatRegistered, decimal? DefaultLaborRate, short? ArrivalReminderLeadDays, bool IsActive,
        DateTimeOffset CreatedAt, PlanRefDto CurrentPlan);

    private sealed record TenantListItemDto(Guid Id, string Name, string Slug, bool IsActive);

    private sealed record PagedDto<T>(List<T> Items, int TotalCount, int Offset, int Limit);

    private sealed record SubscriptionDto(
        Guid Id, Guid SubscriptionPlanId, string PlanCode, Dictionary<string, string> PlanLabel,
        DateOnly ValidFrom, DateOnly? ValidTo, DateOnly? TrialUntil, string? Notes);

    private static string UniqueSlug() => $"test-{Guid.NewGuid():N}"[..20];

    private async Task<(HttpClient Client, PlanDto Plan, short CurrencyId)> SetUpAsync()
    {
        HttpClient client = _factory.CreateClient();
        List<PlanDto>? plans = await client.GetFromJsonAsync<List<PlanDto>>("/api/subscription-plans");
        PlanDto plan = plans!.First(p => p.Code == "starter");
        return (client, plan, plan.CurrencyId);
    }

    private async Task<TenantDto> CreateTenantAsync(HttpClient client, PlanDto plan, short currencyId, string? slug = null)
    {
        HttpResponseMessage created = await client.PostAsJsonAsync("/api/tenants", new
        {
            name = $"Workshop {Guid.NewGuid():N}",
            slug = slug ?? UniqueSlug(),
            subscriptionPlanId = plan.Id,
            defaultCurrencyId = currencyId,
            city = "Kragujevac"
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        return (await created.Content.ReadFromJsonAsync<TenantDto>())!;
    }

    [Fact]
    public async Task ListSubscriptionPlans_ReturnsSeedPlans()
    {
        using HttpClient client = _factory.CreateClient();

        List<PlanDto>? plans = await client.GetFromJsonAsync<List<PlanDto>>("/api/subscription-plans");

        Assert.NotNull(plans);
        Assert.Contains(plans, p => p.Code == "free");
        Assert.All(plans, p => Assert.True(p.Label.ContainsKey("en")));
        Assert.All(plans, p => Assert.Equal(JsonValueKind.Object, p.Features.ValueKind));
    }

    [Fact]
    public async Task CreatePlan_UnknownCurrency_Returns400()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/subscription-plans", new
        {
            code = $"plan_{Guid.NewGuid():N}"[..20],
            label = new Dictionary<string, string> { ["en"] = "Bad currency" },
            price = 10,
            currencyId = short.MaxValue,
            billingPeriodId = 1
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateUpdateAndRetirePlan_FullCycle()
    {
        (HttpClient client, PlanDto seedPlan, short currencyId) = await SetUpAsync();
        string code = $"plan_{Guid.NewGuid():N}"[..20];

        HttpResponseMessage created = await client.PostAsJsonAsync("/api/subscription-plans", new
        {
            code,
            label = new Dictionary<string, string> { ["en"] = "Custom" },
            price = 49.99m,
            currencyId,
            billingPeriodId = seedPlan.BillingPeriodId,
            trialDays = 7,
            features = new { api_access = true }
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        PlanDto? plan = await created.Content.ReadFromJsonAsync<PlanDto>();
        Assert.Equal(7, plan!.TrialDays);
        Assert.True(plan.Features.GetProperty("api_access").GetBoolean());

        HttpResponseMessage duplicate = await client.PostAsJsonAsync("/api/subscription-plans", new
        {
            code,
            label = new Dictionary<string, string> { ["en"] = "Dup" },
            price = 1,
            currencyId,
            billingPeriodId = seedPlan.BillingPeriodId
        });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        HttpResponseMessage updated = await client.PutAsJsonAsync($"/api/subscription-plans/{plan.Id}", new
        {
            label = new Dictionary<string, string> { ["en"] = "Custom v2" },
            price = 59.99m,
            currencyId,
            billingPeriodId = seedPlan.BillingPeriodId
        });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);

        HttpResponseMessage retired = await client.PostAsJsonAsync(
            $"/api/subscription-plans/{plan.Id}/activation", new { isActive = false });
        Assert.Equal(HttpStatusCode.NoContent, retired.StatusCode);

        List<PlanDto>? active = await client.GetFromJsonAsync<List<PlanDto>>("/api/subscription-plans");
        List<PlanDto>? all = await client.GetFromJsonAsync<List<PlanDto>>("/api/subscription-plans?includeInactive=true");
        Assert.DoesNotContain(active!, p => p.Code == code);
        Assert.Contains(all!, p => p.Code == code && !p.IsActive);
    }

    [Fact]
    public async Task CreateTenant_WritesTenantAndInitialSubscriptionAtomically()
    {
        (HttpClient client, PlanDto plan, short currencyId) = await SetUpAsync();

        TenantDto tenant = await CreateTenantAsync(client, plan, currencyId);

        // Audit interceptor + DB defaults filled the audit columns.
        Assert.NotEqual(default, tenant.CreatedAt);
        Assert.Equal(plan.Code, tenant.CurrentPlan.Code);
        Assert.Equal((short)1, tenant.ArrivalReminderLeadDays);

        // The initial subscription period was written in the same transaction.
        List<SubscriptionDto>? history = await client.GetFromJsonAsync<List<SubscriptionDto>>(
            $"/api/tenants/{tenant.Id}/subscriptions");
        SubscriptionDto initial = Assert.Single(history!);
        Assert.Equal(plan.Id, initial.SubscriptionPlanId);
        Assert.Null(initial.ValidTo);
        Assert.NotNull(initial.TrialUntil); // starter has 14 trial days
    }

    [Fact]
    public async Task CreateTenant_DuplicateSlug_Returns409()
    {
        (HttpClient client, PlanDto plan, short currencyId) = await SetUpAsync();
        string slug = UniqueSlug();

        await CreateTenantAsync(client, plan, currencyId, slug);
        HttpResponseMessage duplicate = await client.PostAsJsonAsync("/api/tenants", new
        {
            name = "Duplicate",
            slug,
            subscriptionPlanId = plan.Id,
            defaultCurrencyId = currencyId
        });

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task CreateTenant_UnknownPlanOrBadSlug_Rejected()
    {
        (HttpClient client, PlanDto plan, short currencyId) = await SetUpAsync();

        HttpResponseMessage unknownPlan = await client.PostAsJsonAsync("/api/tenants", new
        {
            name = "X",
            slug = UniqueSlug(),
            subscriptionPlanId = Guid.NewGuid(),
            defaultCurrencyId = currencyId
        });
        HttpResponseMessage badSlug = await client.PostAsJsonAsync("/api/tenants", new
        {
            name = "X",
            slug = "Not A Slug!",
            subscriptionPlanId = plan.Id,
            defaultCurrencyId = currencyId
        });

        Assert.Equal(HttpStatusCode.BadRequest, unknownPlan.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, badSlug.StatusCode);
    }

    [Fact]
    public async Task ListTenants_SearchAndPagingWork()
    {
        (HttpClient client, PlanDto plan, short currencyId) = await SetUpAsync();
        TenantDto tenant = await CreateTenantAsync(client, plan, currencyId);
        await CreateTenantAsync(client, plan, currencyId);

        PagedDto<TenantListItemDto>? found = await client.GetFromJsonAsync<PagedDto<TenantListItemDto>>(
            $"/api/tenants?search={tenant.Slug}");
        Assert.Single(found!.Items);
        Assert.Equal(tenant.Id, found.Items[0].Id);

        PagedDto<TenantListItemDto>? paged = await client.GetFromJsonAsync<PagedDto<TenantListItemDto>>(
            "/api/tenants?limit=1");
        Assert.Single(paged!.Items);
        Assert.True(paged.TotalCount >= 2);
    }

    [Fact]
    public async Task UpdateTenant_ChangesFiscalAndReminderSettings()
    {
        (HttpClient client, PlanDto plan, short currencyId) = await SetUpAsync();
        TenantDto tenant = await CreateTenantAsync(client, plan, currencyId);

        HttpResponseMessage updated = await client.PutAsJsonAsync($"/api/tenants/{tenant.Id}", new
        {
            name = tenant.Name,
            defaultCurrencyId = currencyId,
            taxId = "123456789",
            isVatRegistered = true,
            defaultLaborRate = 35.5m,
            arrivalReminderLeadDays = 2
        });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);

        TenantDto? refreshed = await client.GetFromJsonAsync<TenantDto>($"/api/tenants/{tenant.Id}");
        Assert.Equal("123456789", refreshed!.TaxId);
        Assert.True(refreshed.IsVatRegistered);
        Assert.Equal(35.5m, refreshed.DefaultLaborRate);
        Assert.Equal((short)2, refreshed.ArrivalReminderLeadDays);
    }

    [Fact]
    public async Task DeactivateAndDelete_TenantLifecycle()
    {
        (HttpClient client, PlanDto plan, short currencyId) = await SetUpAsync();
        TenantDto tenant = await CreateTenantAsync(client, plan, currencyId);

        HttpResponseMessage suspended = await client.PostAsJsonAsync(
            $"/api/tenants/{tenant.Id}/activation", new { isActive = false });
        Assert.Equal(HttpStatusCode.NoContent, suspended.StatusCode);
        TenantDto? refreshed = await client.GetFromJsonAsync<TenantDto>($"/api/tenants/{tenant.Id}");
        Assert.False(refreshed!.IsActive);

        HttpResponseMessage deleted = await client.DeleteAsync($"/api/tenants/{tenant.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        // Soft-deleted: invisible to the detail and list endpoints.
        HttpResponseMessage gone = await client.GetAsync($"/api/tenants/{tenant.Id}");
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
        PagedDto<TenantListItemDto>? list = await client.GetFromJsonAsync<PagedDto<TenantListItemDto>>(
            $"/api/tenants?search={tenant.Slug}");
        Assert.Empty(list!.Items);
    }

    [Fact]
    public async Task ChangeSubscription_ClosesCurrentPeriodAndRepointsTheTenant()
    {
        (HttpClient client, PlanDto plan, short currencyId) = await SetUpAsync();
        TenantDto tenant = await CreateTenantAsync(client, plan, currencyId);

        List<PlanDto>? plans = await client.GetFromJsonAsync<List<PlanDto>>("/api/subscription-plans");
        PlanDto newPlan = plans!.First(p => p.Code == "pro");

        HttpResponseMessage changed = await client.PostAsJsonAsync($"/api/tenants/{tenant.Id}/subscriptions",
            new { subscriptionPlanId = newPlan.Id, notes = "upgrade" });
        Assert.Equal(HttpStatusCode.Created, changed.StatusCode);

        List<SubscriptionDto>? history = await client.GetFromJsonAsync<List<SubscriptionDto>>(
            $"/api/tenants/{tenant.Id}/subscriptions");
        Assert.Equal(2, history!.Count);
        SubscriptionDto current = Assert.Single(history, s => s.ValidTo is null);
        Assert.Equal(newPlan.Id, current.SubscriptionPlanId);
        SubscriptionDto closed = Assert.Single(history, s => s.ValidTo is not null);
        Assert.True(closed.ValidTo >= closed.ValidFrom);

        TenantDto? refreshed = await client.GetFromJsonAsync<TenantDto>($"/api/tenants/{tenant.Id}");
        Assert.Equal("pro", refreshed!.CurrentPlan.Code);
    }
}
