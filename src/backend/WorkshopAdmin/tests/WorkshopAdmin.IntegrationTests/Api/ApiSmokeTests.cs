using System.Net;
using WorkshopAdmin.IntegrationTests.Postgres;
using Xunit;

namespace WorkshopAdmin.IntegrationTests.Api;

[Collection(DatabaseCollection.Name)]
public sealed class ApiSmokeTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Host_Boots_And_ServesOpenApi()
    {
        using ApiFactory factory = new(fixture);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UnknownRoute_Returns404()
    {
        using ApiFactory factory = new(fixture);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
