using FluentValidation.Results;
using WorkshopAdmin.Modules.Codebook.Features;
using Xunit;

namespace WorkshopAdmin.UnitTests.Codebook;

public class CodebookValidatorTests
{
    private static readonly Dictionary<string, string> ValidLabel = new() { ["en"] = "Test", ["sr"] = "Test" };

    [Theory]
    [InlineData("oil_change", true)]
    [InlineData("ac2", true)]
    [InlineData("", false)]
    [InlineData("Oil-Change", false)]
    [InlineData("UPPER", false)]
    [InlineData("has space", false)]
    public void CreateCodebookEntry_CodePattern(string code, bool expectedValid)
    {
        CreateCodebookEntryRequestValidator validator = new();

        ValidationResult result = validator.Validate(new CreateCodebookEntryRequest(code, ValidLabel));

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Fact]
    public void CreateCodebookEntry_LabelWithoutEnglish_Fails()
    {
        CreateCodebookEntryRequestValidator validator = new();

        ValidationResult result = validator.Validate(
            new CreateCodebookEntryRequest("ok_code", new Dictionary<string, string> { ["sr"] = "Samo srpski" }));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void UpdateCodebookEntry_ValidLabel_Passes()
    {
        UpdateCodebookEntryRequestValidator validator = new();

        Assert.True(validator.Validate(new UpdateCodebookEntryRequest(ValidLabel, 5)).IsValid);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(20, true)]
    [InlineData(100, true)]
    [InlineData(-1, false)]
    [InlineData(101, false)]
    public void CreateTaxRate_RateRange(decimal rate, bool expectedValid)
    {
        CreateTaxRateRequestValidator validator = new();

        ValidationResult result = validator.Validate(new CreateTaxRateRequest("vat_test", ValidLabel, rate));

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Theory]
    [InlineData((short)30, true)]
    [InlineData(null, true)]
    [InlineData((short)0, false)]
    [InlineData((short)-15, false)]
    public void CreateServiceType_DurationMustBePositiveWhenSet(short? duration, bool expectedValid)
    {
        CreateServiceTypeRequestValidator validator = new();

        ValidationResult result = validator.Validate(
            new CreateServiceTypeRequest("svc_test", ValidLabel, duration));

        Assert.Equal(expectedValid, result.IsValid);
    }
}
