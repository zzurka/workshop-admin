using System.Text.Json;
using FluentValidation.Results;
using WorkshopAdmin.Modules.Tenants.Features.SubscriptionPlans;
using WorkshopAdmin.Modules.Tenants.Features.Tenants;
using Xunit;

namespace WorkshopAdmin.UnitTests.Tenants;

public class TenantValidatorTests
{
    private static readonly Dictionary<string, string> ValidLabel = new() { ["en"] = "Test" };

    [Theory]
    [InlineData("workshop-kragujevac", true)]
    [InlineData("ws1", true)]
    [InlineData("", false)]
    [InlineData("Workshop", false)]
    [InlineData("has space", false)]
    [InlineData("pod_vlakom", false)]
    public void CreateTenant_SlugPattern(string slug, bool expectedValid)
    {
        CreateTenantRequestValidator validator = new();

        ValidationResult result = validator.Validate(
            new CreateTenantRequest("Test Workshop", slug, Guid.NewGuid(), 1));

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Theory]
    [InlineData("123456789", true)]
    [InlineData(null, true)]
    [InlineData("12345678", false)]
    [InlineData("12345678a", false)]
    public void UpdateTenant_PibMustBeNineDigitsWhenSet(string? taxId, bool expectedValid)
    {
        UpdateTenantRequestValidator validator = new();

        ValidationResult result = validator.Validate(
            new UpdateTenantRequest("Test Workshop", 1, TaxId: taxId));

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Theory]
    [InlineData((short)0, true)]
    [InlineData((short)2, true)]
    [InlineData(null, true)]
    [InlineData((short)31, false)]
    [InlineData((short)-1, false)]
    public void UpdateTenant_ArrivalReminderLeadDaysRange(short? leadDays, bool expectedValid)
    {
        UpdateTenantRequestValidator validator = new();

        ValidationResult result = validator.Validate(
            new UpdateTenantRequest("Test Workshop", 1, ArrivalReminderLeadDays: leadDays));

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Fact]
    public void CreateSubscriptionPlan_FeaturesMustBeJsonObject()
    {
        CreateSubscriptionPlanRequestValidator validator = new();

        CreateSubscriptionPlanRequest ValidRequest(JsonElement? features) =>
            new("plan_test", ValidLabel, null, 10m, 1, 1, Features: features);

        Assert.True(validator.Validate(ValidRequest(null)).IsValid);
        Assert.True(validator.Validate(ValidRequest(JsonDocument.Parse("""{"api": true}""").RootElement)).IsValid);
        Assert.False(validator.Validate(ValidRequest(JsonDocument.Parse("[1,2]").RootElement)).IsValid);
    }

    [Fact]
    public void CreateSubscriptionPlan_NegativePrice_Fails()
    {
        CreateSubscriptionPlanRequestValidator validator = new();

        ValidationResult result = validator.Validate(
            new CreateSubscriptionPlanRequest("plan_test", ValidLabel, null, -1m, 1, 1));

        Assert.False(result.IsValid);
    }
}
