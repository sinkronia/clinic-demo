using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Clinic.IntegrationTests;

[Collection("api")]
public class AppointmentsTests(ApiFixture api)
{
    // The fixture clock is 2026-06-15T12:00Z; these slots are on the next day, so they are in the future.
    private static readonly DateTimeOffset _day = new(2026, 6, 16, 0, 0, 0, TimeSpan.Zero);

    private static DateTimeOffset At(int hour, int minute = 0) => _day.AddHours(hour).AddMinutes(minute);

    private static object Body(Guid patientId, Guid chairId, DateTimeOffset startsAt, DateTimeOffset endsAt) =>
        new { patientId, dentalChairId = chairId, startsAt, endsAt };

    private static Task<HttpResponseMessage> PostAsync(HttpClient client, object body, string? key = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/appointments") { Content = JsonContent.Create(body) };
        request.Headers.Add("Idempotency-Key", key ?? Guid.NewGuid().ToString());
        return client.SendAsync(request);
    }

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage response) =>
        JsonElement.Parse(await response.Content.ReadAsStringAsync());

    private static async Task<string?> CodeAsync(HttpResponseMessage response) =>
        (await JsonAsync(response)).GetProperty("code").GetString();

    private static async Task<Guid> CreatePatientAsync(HttpClient client)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/patients")
        {
            Content = JsonContent.Create(new
            {
                fullName = "Test Patient One",
                documentNumber = Random.Shared.Next(1_000_000, 9_999_999).ToString(System.Globalization.CultureInfo.InvariantCulture),
                phone = "+59170000001",
                birthDate = "1990-05-20",
            }),
        };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var response = await client.SendAsync(request);
        return (await JsonAsync(response)).GetProperty("id").GetGuid();
    }

    private static async Task<Guid> BookAsync(HttpClient client, Guid patientId, Guid chairId, DateTimeOffset start, DateTimeOffset end)
    {
        var response = await PostAsync(client, Body(patientId, chairId, start, end));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await JsonAsync(response)).GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Create_valid_appointment_returns_201_scheduled()
    {
        var client = api.CreateClient(Guid.NewGuid());
        var patient = await CreatePatientAsync(client);
        var chair = Guid.NewGuid();

        var response = await PostAsync(client, Body(patient, chair, At(10), At(10, 30)));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await JsonAsync(response);
        Assert.Equal("Scheduled", json.GetProperty("status").GetString());
        Assert.Equal(patient, json.GetProperty("patientId").GetGuid());
        Assert.Equal(chair, json.GetProperty("dentalChairId").GetGuid());
    }

    [Fact]
    public async Task Create_overlapping_on_same_chair_returns_409_overlap()
    {
        var client = api.CreateClient(Guid.NewGuid());
        var patient = await CreatePatientAsync(client);
        var chair = Guid.NewGuid();
        await BookAsync(client, patient, chair, At(10), At(11));

        var response = await PostAsync(client, Body(patient, chair, At(10, 30), At(11, 30)));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("appointment.overlap", await CodeAsync(response));
    }

    [Fact]
    public async Task Create_contained_within_existing_on_same_chair_returns_409_overlap()
    {
        var client = api.CreateClient(Guid.NewGuid());
        var patient = await CreatePatientAsync(client);
        var chair = Guid.NewGuid();
        await BookAsync(client, patient, chair, At(10), At(12));

        var response = await PostAsync(client, Body(patient, chair, At(10, 30), At(11)));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("appointment.overlap", await CodeAsync(response));
    }

    [Fact]
    public async Task Create_touching_boundaries_on_same_chair_do_not_overlap()
    {
        var client = api.CreateClient(Guid.NewGuid());
        var patient = await CreatePatientAsync(client);
        var chair = Guid.NewGuid();

        await BookAsync(client, patient, chair, At(10), At(10, 30));
        await BookAsync(client, patient, chair, At(10, 30), At(11));
        await BookAsync(client, patient, chair, At(9, 30), At(10));
    }

    [Fact]
    public async Task Create_same_time_on_different_chair_is_allowed()
    {
        var client = api.CreateClient(Guid.NewGuid());
        var patient = await CreatePatientAsync(client);

        await BookAsync(client, patient, Guid.NewGuid(), At(10), At(11));
        await BookAsync(client, patient, Guid.NewGuid(), At(10), At(11));
    }

    [Fact]
    public async Task Cancelled_appointment_frees_the_slot()
    {
        var client = api.CreateClient(Guid.NewGuid());
        var patient = await CreatePatientAsync(client);
        var chair = Guid.NewGuid();
        var id = await BookAsync(client, patient, chair, At(10), At(11));

        var cancel = await client.PostAsync($"/appointments/{id}/cancel", content: null);
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);

        await BookAsync(client, patient, chair, At(10), At(11));
    }

    [Fact]
    public async Task Cancel_returns_cancelled_status_and_is_idempotent()
    {
        var client = api.CreateClient(Guid.NewGuid());
        var patient = await CreatePatientAsync(client);
        var id = await BookAsync(client, patient, Guid.NewGuid(), At(10), At(11));

        var first = await client.PostAsync($"/appointments/{id}/cancel", content: null);
        var second = await client.PostAsync($"/appointments/{id}/cancel", content: null);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal("Cancelled", (await JsonAsync(first)).GetProperty("status").GetString());
        Assert.Equal("Cancelled", (await JsonAsync(second)).GetProperty("status").GetString());
    }

    [Fact]
    public async Task Cancel_unknown_id_returns_404_not_found()
    {
        var client = api.CreateClient(Guid.NewGuid());

        var response = await client.PostAsync($"/appointments/{Guid.NewGuid()}/cancel", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("appointment.not_found", await CodeAsync(response));
    }

    [Fact]
    public async Task Cancel_appointment_of_another_tenant_returns_404()
    {
        var tenantA = api.CreateClient(Guid.NewGuid());
        var tenantB = api.CreateClient(Guid.NewGuid());
        var id = await BookAsync(tenantA, await CreatePatientAsync(tenantA), Guid.NewGuid(), At(10), At(11));

        var response = await tenantB.PostAsync($"/appointments/{id}/cancel", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("appointment.not_found", await CodeAsync(response));
    }

    [Fact]
    public async Task Tenant_isolation_tenant_b_cannot_see_tenant_a_appointments_or_conflict_with_them()
    {
        var tenantA = api.CreateClient(Guid.NewGuid());
        var tenantB = api.CreateClient(Guid.NewGuid());
        var chair = Guid.NewGuid();
        await BookAsync(tenantA, await CreatePatientAsync(tenantA), chair, At(10), At(11));

        Assert.Equal(1, (await tenantA.GetFromJsonAsync<JsonElement>("/appointments?date=2026-06-16")).GetArrayLength());
        Assert.Equal(0, (await tenantB.GetFromJsonAsync<JsonElement>("/appointments?date=2026-06-16")).GetArrayLength());

        // Same chair id and time in another tenant is not an overlap.
        await BookAsync(tenantB, await CreatePatientAsync(tenantB), chair, At(10), At(11));
    }

    [Fact]
    public async Task Create_with_patient_of_another_tenant_returns_404_patient_not_found()
    {
        var tenantA = api.CreateClient(Guid.NewGuid());
        var tenantB = api.CreateClient(Guid.NewGuid());
        var patientOfA = await CreatePatientAsync(tenantA);

        var response = await PostAsync(tenantB, Body(patientOfA, Guid.NewGuid(), At(10), At(11)));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("patient.not_found", await CodeAsync(response));
    }

    [Fact]
    public async Task Create_with_unknown_patient_returns_404_patient_not_found()
    {
        var client = api.CreateClient(Guid.NewGuid());

        var response = await PostAsync(client, Body(Guid.NewGuid(), Guid.NewGuid(), At(10), At(11)));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("patient.not_found", await CodeAsync(response));
    }

    [Fact]
    public async Task Create_with_archived_patient_returns_404_patient_not_found()
    {
        var client = api.CreateClient(Guid.NewGuid());
        var patient = await CreatePatientAsync(client);
        await api.ArchivePatientAsync(patient);

        var response = await PostAsync(client, Body(patient, Guid.NewGuid(), At(10), At(11)));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("patient.not_found", await CodeAsync(response));
    }

    [Fact]
    public async Task Create_starting_in_the_past_returns_400_in_past()
    {
        var client = api.CreateClient(Guid.NewGuid());
        var patient = await CreatePatientAsync(client);
        var past = api.Time.GetUtcNow().AddHours(-1);

        var response = await PostAsync(client, Body(patient, Guid.NewGuid(), past, past.AddMinutes(30)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("appointment.in_past", await CodeAsync(response));
    }

    [Fact]
    public async Task Create_starting_exactly_now_is_allowed()
    {
        var client = api.CreateClient(Guid.NewGuid());
        var patient = await CreatePatientAsync(client);
        var now = api.Time.GetUtcNow();

        await BookAsync(client, patient, Guid.NewGuid(), now, now.AddMinutes(30));
    }

    [Theory]
    [InlineData(14, "too_short")]
    [InlineData(241, "too_long")]
    [InlineData(0, "must_be_after_start")]
    [InlineData(-30, "must_be_after_start")]
    public async Task Create_with_invalid_duration_returns_400_with_field_error_code(int minutes, string expected)
    {
        var client = api.CreateClient(Guid.NewGuid());
        var patient = await CreatePatientAsync(client);

        var response = await PostAsync(client, Body(patient, Guid.NewGuid(), At(10), At(10).AddMinutes(minutes)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await JsonAsync(response);
        Assert.Equal("validation.failed", json.GetProperty("code").GetString());
        Assert.Equal(expected, json.GetProperty("errors").GetProperty("endsAt")[0].GetString());
    }

    [Fact]
    public async Task Create_validation_runs_before_the_past_check()
    {
        var client = api.CreateClient(Guid.NewGuid());
        var past = api.Time.GetUtcNow().AddDays(-1);

        var response = await PostAsync(client, Body(Guid.NewGuid(), Guid.NewGuid(), past, past));

        Assert.Equal("validation.failed", await CodeAsync(response));
    }

    [Fact]
    public async Task Create_with_same_key_and_same_body_replays_the_stored_response()
    {
        var client = api.CreateClient(Guid.NewGuid());
        var body = Body(await CreatePatientAsync(client), Guid.NewGuid(), At(10), At(11));
        var key = Guid.NewGuid().ToString();

        var first = await PostAsync(client, body, key);
        var second = await PostAsync(client, body, key);

        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.Equal(await first.Content.ReadAsStringAsync(), await second.Content.ReadAsStringAsync());
        Assert.True(second.Headers.Contains("Idempotency-Replayed"));
        Assert.Equal(1, (await client.GetFromJsonAsync<JsonElement>("/appointments?date=2026-06-16")).GetArrayLength());
    }

    [Fact]
    public async Task Create_with_same_key_and_different_body_returns_409_key_reuse()
    {
        var client = api.CreateClient(Guid.NewGuid());
        var patient = await CreatePatientAsync(client);
        var key = Guid.NewGuid().ToString();
        await PostAsync(client, Body(patient, Guid.NewGuid(), At(10), At(11)), key);

        var reuse = await PostAsync(client, Body(patient, Guid.NewGuid(), At(12), At(13)), key);

        Assert.Equal(HttpStatusCode.Conflict, reuse.StatusCode);
        Assert.Equal("idempotency.key_reuse", await CodeAsync(reuse));
    }

    [Fact]
    public async Task Create_without_idempotency_key_returns_400()
    {
        var client = api.CreateClient(Guid.NewGuid());

        var response = await client.PostAsJsonAsync("/appointments", Body(Guid.NewGuid(), Guid.NewGuid(), At(10), At(11)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("idempotency.key_missing", await CodeAsync(response));
    }

    [Fact]
    public async Task Request_without_tenant_header_returns_400_tenant_missing()
    {
        var client = api.CreateClient(tenantId: null);

        var response = await client.GetAsync("/appointments?date=2026-06-16");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("tenant.missing", await CodeAsync(response));
    }

    [Fact]
    public async Task List_returns_the_utc_day_ordered_by_start_including_cancelled()
    {
        var client = api.CreateClient(Guid.NewGuid());
        var patient = await CreatePatientAsync(client);
        var chair = Guid.NewGuid();
        var later = await BookAsync(client, patient, chair, At(15), At(16));
        var earlier = await BookAsync(client, patient, chair, At(9), At(10));
        var cancelled = await BookAsync(client, patient, chair, At(12), At(13));
        await client.PostAsync($"/appointments/{cancelled}/cancel", content: null);
        await BookAsync(client, patient, chair, At(23, 30), At(24, 0).AddMinutes(0)); // 23:30-00:00, starts on 16th
        await BookAsync(client, patient, chair, At(24), At(25)); // starts on the 17th

        var list = await client.GetFromJsonAsync<JsonElement>("/appointments?date=2026-06-16");

        var items = list.EnumerateArray().ToList();
        Assert.Equal(4, items.Count);
        Assert.Equal(earlier, items[0].GetProperty("id").GetGuid());
        Assert.Equal(cancelled, items[1].GetProperty("id").GetGuid());
        Assert.Equal("Cancelled", items[1].GetProperty("status").GetString());
        Assert.Equal(later, items[2].GetProperty("id").GetGuid());
    }

    [Theory]
    [InlineData("", "required")]
    [InlineData("16-06-2026", "invalid_format")]
    [InlineData("nope", "invalid_format")]
    public async Task List_with_missing_or_bad_date_returns_400_validation_failed(string date, string expected)
    {
        var client = api.CreateClient(Guid.NewGuid());

        var response = await client.GetAsync($"/appointments?date={date}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await JsonAsync(response);
        Assert.Equal("validation.failed", json.GetProperty("code").GetString());
        Assert.Equal(expected, json.GetProperty("errors").GetProperty("date")[0].GetString());
    }
}
