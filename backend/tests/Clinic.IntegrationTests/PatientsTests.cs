using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Clinic.IntegrationTests;

[Collection("api")]
public class PatientsTests(ApiFixture api)
{
    private static string NewDocument() => Random.Shared.Next(1_000_000, 9_999_999).ToString(System.Globalization.CultureInfo.InvariantCulture);

    private static object NewPatient(string? document = null, string name = "Test Patient One") => new
    {
        fullName = name,
        documentNumber = document ?? NewDocument(),
        phone = "+59170000001",
        birthDate = "1990-05-20",
    };

    private static Task<HttpResponseMessage> PostAsync(HttpClient client, object body, string? key = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/patients") { Content = JsonContent.Create(body) };
        request.Headers.Add("Idempotency-Key", key ?? Guid.NewGuid().ToString());
        return client.SendAsync(request);
    }

    private static async Task<string?> CodeAsync(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("code").GetString();
    }

    [Fact]
    public async Task Create_then_list_returns_the_patient()
    {
        var client = api.CreateClient(Guid.NewGuid());
        var document = NewDocument();

        var created = await PostAsync(client, NewPatient(document));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var list = await client.GetFromJsonAsync<JsonElement>("/patients");
        var only = Assert.Single(list.EnumerateArray());
        Assert.Equal(document, only.GetProperty("documentNumber").GetString());
        Assert.Equal("Test Patient One", only.GetProperty("fullName").GetString());
    }

    [Fact]
    public async Task List_search_matches_name_document_and_phone()
    {
        var client = api.CreateClient(Guid.NewGuid());
        await PostAsync(client, NewPatient("7000001", "Alpha Tester"));
        await PostAsync(client, new { fullName = "Beta Tester", documentNumber = "7000002", phone = "+59171111111", birthDate = "1985-01-01" });

        async Task<int> CountAsync(string search) =>
            (await client.GetFromJsonAsync<JsonElement>($"/patients?search={search}")).GetArrayLength();

        Assert.Equal(1, await CountAsync("alpha"));
        Assert.Equal(1, await CountAsync("7000002"));
        Assert.Equal(1, await CountAsync("71111111"));
        Assert.Equal(2, await CountAsync("tester"));
        Assert.Equal(0, await CountAsync("nobody"));
    }

    [Fact]
    public async Task Tenant_isolation_tenant_b_cannot_see_tenant_a_patients()
    {
        var tenantA = api.CreateClient(Guid.NewGuid());
        var tenantB = api.CreateClient(Guid.NewGuid());
        var document = NewDocument();

        Assert.Equal(HttpStatusCode.Created, (await PostAsync(tenantA, NewPatient(document))).StatusCode);

        Assert.Equal(1, (await tenantA.GetFromJsonAsync<JsonElement>("/patients")).GetArrayLength());
        Assert.Equal(0, (await tenantB.GetFromJsonAsync<JsonElement>("/patients")).GetArrayLength());

        // The same document is allowed in another tenant.
        Assert.Equal(HttpStatusCode.Created, (await PostAsync(tenantB, NewPatient(document))).StatusCode);
    }

    [Fact]
    public async Task Create_duplicate_document_in_same_tenant_returns_409_with_code()
    {
        var client = api.CreateClient(Guid.NewGuid());
        var document = NewDocument();
        await PostAsync(client, NewPatient(document));

        var duplicate = await PostAsync(client, NewPatient(document, "Another Person"));

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("patient.duplicate_document", await CodeAsync(duplicate));
    }

    [Fact]
    public async Task Create_with_same_key_and_same_body_replays_the_stored_response()
    {
        var client = api.CreateClient(Guid.NewGuid());
        var body = NewPatient();
        var key = Guid.NewGuid().ToString();

        var first = await PostAsync(client, body, key);
        var second = await PostAsync(client, body, key);

        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.Equal(await first.Content.ReadAsStringAsync(), await second.Content.ReadAsStringAsync());
        Assert.True(second.Headers.Contains("Idempotency-Replayed"));
        Assert.Equal(1, (await client.GetFromJsonAsync<JsonElement>("/patients")).GetArrayLength());
    }

    [Fact]
    public async Task Create_with_same_key_and_different_body_returns_409_key_reuse()
    {
        var client = api.CreateClient(Guid.NewGuid());
        var key = Guid.NewGuid().ToString();
        await PostAsync(client, NewPatient(), key);

        var reuse = await PostAsync(client, NewPatient(), key);

        Assert.Equal(HttpStatusCode.Conflict, reuse.StatusCode);
        Assert.Equal("idempotency.key_reuse", await CodeAsync(reuse));
    }

    [Fact]
    public async Task Create_without_idempotency_key_returns_400()
    {
        var client = api.CreateClient(Guid.NewGuid());

        var response = await client.PostAsJsonAsync("/patients", NewPatient());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("idempotency.key_missing", await CodeAsync(response));
    }

    [Fact]
    public async Task Create_with_invalid_body_returns_400_with_field_error_codes()
    {
        var client = api.CreateClient(Guid.NewGuid());

        var response = await PostAsync(client, new { fullName = "", documentNumber = "x", phone = "+59170000001", birthDate = "1990-05-20" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("validation.failed", doc.RootElement.GetProperty("code").GetString());
        Assert.Equal("required", doc.RootElement.GetProperty("errors").GetProperty("fullName")[0].GetString());
        Assert.Equal("invalid_format", doc.RootElement.GetProperty("errors").GetProperty("documentNumber")[0].GetString());
    }

    [Fact]
    public async Task Request_without_tenant_header_returns_400_tenant_missing()
    {
        var client = api.CreateClient(tenantId: null);

        var response = await client.GetAsync("/patients");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("tenant.missing", await CodeAsync(response));
    }
}
