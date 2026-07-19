using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using WorkshopAdmin.IntegrationTests.Postgres;
using WorkshopAdmin.Modules.Codebook.Contracts;
using Xunit;

namespace WorkshopAdmin.IntegrationTests.Api;

[Collection(DatabaseCollection.Name)]
public sealed class CodebookApiTests(PostgresFixture fixture) : IDisposable
{
    private readonly ApiFactory _factory = new(fixture);

    public void Dispose() => _factory.Dispose();

    private sealed record EntryDto(short Id, string Code, Dictionary<string, string> Label, short SortOrder, bool IsActive);

    private sealed record TaxRateDto(short Id, string Code, Dictionary<string, string> Label, decimal Rate, short SortOrder, bool IsActive);

    private sealed record ServiceTypeDto(short Id, string Code, Dictionary<string, string> Label, short? DefaultDurationMin, short SortOrder, bool IsActive);

    private static string UniqueCode() => $"test_{Guid.NewGuid():N}"[..20];

    [Fact]
    public async Task ListTypes_ReturnsAllTwentyCodebooks()
    {
        using HttpClient client = _factory.CreateClient();

        List<string>? types = await client.GetFromJsonAsync<List<string>>("/api/codebook");

        Assert.NotNull(types);
        Assert.Equal(20, types.Count);
        Assert.Contains("currencies", types);
        Assert.Contains("tax_rates", types);
    }

    [Fact]
    public async Task ListKnownType_ReturnsSeedRowsWithJsonbLabels()
    {
        using HttpClient client = _factory.CreateClient();

        List<EntryDto>? currencies = await client.GetFromJsonAsync<List<EntryDto>>("/api/codebook/currencies");

        Assert.NotNull(currencies);
        Assert.Contains(currencies, c => c.Code == "EUR");
        Assert.All(currencies, c => Assert.True(c.Label.ContainsKey("en")));
    }

    [Fact]
    public async Task UnknownType_Returns404()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/codebook/no_such_codebook");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidatesCache_NextListSeesTheNewRow()
    {
        using HttpClient client = _factory.CreateClient();
        string code = UniqueCode();

        // Prime the cache first, then write.
        await client.GetFromJsonAsync<List<EntryDto>>("/api/codebook/fuel_types");

        HttpResponseMessage created = await client.PostAsJsonAsync("/api/codebook/fuel_types",
            new { code, label = new Dictionary<string, string> { ["en"] = "Test fuel", ["sr"] = "Test gorivo" } });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.NotNull(created.Headers.Location);

        List<EntryDto>? fuelTypes = await client.GetFromJsonAsync<List<EntryDto>>("/api/codebook/fuel_types");
        Assert.NotNull(fuelTypes);
        Assert.Contains(fuelTypes, f => f.Code == code);
    }

    [Fact]
    public async Task Create_DuplicateCode_Returns409()
    {
        using HttpClient client = _factory.CreateClient();
        string code = UniqueCode();
        object body = new { code, label = new Dictionary<string, string> { ["en"] = "Dup" } };

        await client.PostAsJsonAsync("/api/codebook/fuel_types", body);
        HttpResponseMessage duplicate = await client.PostAsJsonAsync("/api/codebook/fuel_types", body);

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidCodeOrLabel_Returns400()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage badCode = await client.PostAsJsonAsync("/api/codebook/fuel_types",
            new { code = "Not Valid!", label = new Dictionary<string, string> { ["en"] = "X" } });
        HttpResponseMessage noEnglish = await client.PostAsJsonAsync("/api/codebook/fuel_types",
            new { code = UniqueCode(), label = new Dictionary<string, string> { ["sr"] = "Samo srpski" } });

        Assert.Equal(HttpStatusCode.BadRequest, badCode.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, noEnglish.StatusCode);
    }

    [Fact]
    public async Task Update_ChangesLabelAndSortOrder()
    {
        using HttpClient client = _factory.CreateClient();
        string code = UniqueCode();

        HttpResponseMessage created = await client.PostAsJsonAsync("/api/codebook/fuel_types",
            new { code, label = new Dictionary<string, string> { ["en"] = "Before" } });
        EntryDto? entry = await created.Content.ReadFromJsonAsync<EntryDto>();

        HttpResponseMessage updated = await client.PutAsJsonAsync($"/api/codebook/fuel_types/{entry!.Id}",
            new { label = new Dictionary<string, string> { ["en"] = "After" }, sortOrder = 42 });

        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        List<EntryDto>? fuelTypes = await client.GetFromJsonAsync<List<EntryDto>>("/api/codebook/fuel_types");
        EntryDto refreshed = Assert.Single(fuelTypes!, f => f.Code == code);
        Assert.Equal("After", refreshed.Label["en"]);
        Assert.Equal(42, refreshed.SortOrder);
    }

    [Fact]
    public async Task Deactivation_HidesEntryFromDefaultList()
    {
        using HttpClient client = _factory.CreateClient();
        string code = UniqueCode();

        HttpResponseMessage created = await client.PostAsJsonAsync("/api/codebook/fuel_types",
            new { code, label = new Dictionary<string, string> { ["en"] = "Hide me" } });
        EntryDto? entry = await created.Content.ReadFromJsonAsync<EntryDto>();

        HttpResponseMessage deactivated = await client.PostAsJsonAsync(
            $"/api/codebook/fuel_types/{entry!.Id}/activation", new { isActive = false });
        Assert.Equal(HttpStatusCode.NoContent, deactivated.StatusCode);

        List<EntryDto>? active = await client.GetFromJsonAsync<List<EntryDto>>("/api/codebook/fuel_types");
        List<EntryDto>? all = await client.GetFromJsonAsync<List<EntryDto>>("/api/codebook/fuel_types?includeInactive=true");

        Assert.DoesNotContain(active!, f => f.Code == code);
        Assert.Contains(all!, f => f.Code == code && !f.IsActive);
    }

    [Fact]
    public async Task TaxRates_DedicatedSlice_CarriesTheRate()
    {
        using HttpClient client = _factory.CreateClient();
        string code = UniqueCode();

        HttpResponseMessage created = await client.PostAsJsonAsync("/api/codebook/tax_rates",
            new { code, label = new Dictionary<string, string> { ["en"] = "Test VAT" }, rate = 15.5m });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        TaxRateDto? rate = await created.Content.ReadFromJsonAsync<TaxRateDto>();
        Assert.Equal(15.5m, rate!.Rate);

        HttpResponseMessage updated = await client.PutAsJsonAsync($"/api/codebook/tax_rates/{rate.Id}",
            new { label = new Dictionary<string, string> { ["en"] = "Test VAT" }, rate = 12m });
        TaxRateDto? updatedRate = await updated.Content.ReadFromJsonAsync<TaxRateDto>();
        Assert.Equal(12m, updatedRate!.Rate);

        // The generic read slice still lists it (base fields).
        List<EntryDto>? listed = await client.GetFromJsonAsync<List<EntryDto>>("/api/codebook/tax_rates");
        Assert.Contains(listed!, r => r.Code == code);
    }

    [Fact]
    public async Task ServiceTypes_DedicatedSlice_CarriesTheDuration()
    {
        using HttpClient client = _factory.CreateClient();
        string code = UniqueCode();

        HttpResponseMessage created = await client.PostAsJsonAsync("/api/codebook/service_types",
            new { code, label = new Dictionary<string, string> { ["en"] = "Test service" }, defaultDurationMin = 45 });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        ServiceTypeDto? serviceType = await created.Content.ReadFromJsonAsync<ServiceTypeDto>();
        Assert.Equal((short)45, serviceType!.DefaultDurationMin);
    }

    [Fact]
    public async Task CodebookLookup_Contract_ResolvesByCodeAndId()
    {
        await using AsyncServiceScope scope = _factory.Services.CreateAsyncScope();
        ICodebookLookup lookup = scope.ServiceProvider.GetRequiredService<ICodebookLookup>();

        short? id = await lookup.GetIdByCodeAsync(CodebookTypes.Currencies, "EUR");
        Assert.NotNull(id);

        CodebookEntryRef? entry = await lookup.GetByIdAsync(CodebookTypes.Currencies, id.Value);
        Assert.NotNull(entry);
        Assert.Equal("EUR", entry.Code);
        Assert.True(entry.IsActive);

        Assert.Null(await lookup.GetIdByCodeAsync("no_such_type", "EUR"));
        Assert.Null(await lookup.GetIdByCodeAsync(CodebookTypes.Currencies, "XXX"));
    }
}
