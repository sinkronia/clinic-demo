using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Clinic.IntegrationTests;

[Collection("api")]
public class AppointmentsTests(ApiFixture api)
{
    private static readonly DateOnly _day = new(2026, 6, 16);

    private static DateTimeOffset At(int hour, int minute = 0) =>
        new(_day.Year, _day.Month, _day.Day, hour, minute, 0, TimeSpan.Zero);

    private static string NewDocument() =>
        Random.Shared.Next(1_000_000, 9_999_999).ToString(System.Globalization.CultureInfo.InvariantCulture);

    private static object NewPatient() => new
    {
        fullName = "Test Patient One",
        documentNumber = NewDocument(),
        phone = "+59170000001",
        birthDate = "1990-05-20",
    };

    private static object NewAppointment(Guid patientId, Guid dentalChairId, DateTimeOffset startsAt, int minutes = 30) => new
    {
        patientId,
        dentalChairId,
        startsAt,
        endsAt = startsAt.AddMinutes(minutes),
    };

    private static async Task<Guid> CreatePatientAsync(HttpClient client)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/patients") { Content = JsonContent.Create(NewPatient()) };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("id").GetGuid();
    }

    private static Task<HttpResponseMessage> PostAppointmentAsync(HttpClient client, object body, string? key = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/appointments") { Content = JsonContent.Create(body) };
        request.Headers.Add("Idempotency-Key", key ?? Guid.NewGuid().ToString());
        return client.SendAsync(request);
    }

    private static async Task<Guid> CreateAppointmentAsync(
        HttpClient client, Guid patientId, Guid dentalChairId, DateTimeOffset startsAt, int minutes = 30)
    {
        var response = await PostAppointmentAsync(client, NewAppointment(patientId, dentalChairId, startsAt, minutes));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<string?> CodeAsync(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("code").GetString();
    }

    [Fact]
    public async Task Create_then_list_returns_the_appointment()
    {
        var client = api.CreateClient(Guid.NewGuid());
        var patient = await CreatePatientAsync(client);
        var chair = Guid.NewGuid();

        var created = await PostAppointmentAsync(client, NewAppointment(patient, chair, At(10)));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var list = await client.GetFromJsonAsync<JsonElement>($"/appointments?date={_day:yyyy-MM-dd}");
        var only = Assert.Single(list.EnumerateArray());
        Assert.Equal(chair, only.GetProperty("dentalChairId").GetGuid());
        Assert.Equal(patient, only.GetProperty("patientId").GetGuid());
        Assert.Equal("Scheduled", only.GetProperty("status").GetString());
    }

    [Fact]
    public async Task List_returns_only_appointments_starting_on_the_requested_date()
    {
        var client = api.CreateClient(Guid.NewGuid());
        var patient = await CreatePatientAsync(client);
        var chair = Guid.NewGuid();

        await CreateAppointmentAsync(client, patient, chair, At(10));
        await CreateAppointmentAsync(client, patient, chair, At(10).AddDays(1));

        var list = await client.GetFromJsonAsync<JsonElement>($"/appointments?date={_day:yyyy-MM-dd}");
        var only = Assert.Single(list.EnumerateArray());
        Assert.Equal(At(10), only.GetProperty("startsAt").GetDateTimeOffset());
    }

    [Fact]
    public async Task List_without_date_returns_400_validation_failed()
    {
        var client = api.CreateClient(Guid.NewGuid());

        var response = await client.GetAsync("/appointments");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("validation.failed", await CodeAsync(response));
    }

    [Fact]
    public async Task Create_touching_boundaries_on_the_same_chair_is_allowed()
    {
        var client = api.CreateClient(Guid.NewGuid());
        var patient = await CreatePatientAsync(client);
        var chair = Guid.NewGuid();

        await CreateAppointmentAsync(client, patient, chair, At(10), 30);
        var second = await PostAppointmentAsync(client, NewAppointment(patient, chair, At(10, 30), 30));

        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
    }

    [Fact]
    public async Task Create_overlapping_on_the_same_chair_returns_409_overlap()
    {
        var client = api.CreateClient(Guid.NewGuid());
        var patient = await CreatePatientAsync(client);
        var chair = Guid.NewGuid();
        await CreateAppointmentAsync(client, patient, chair, At(10), 30);

        var overlap = await PostAppointmentAsync(client, NewAppointment(patient, chair, At(10, 15), 30));

        Assert.Equal(HttpStatusCode.Conflict, overlap.StatusCode);
        Assert.Equal("appointment.overlap", await CodeAsync(overlap));
    }

    [Fact]
    public async Task Create_same_time_on_a_different_chair_is_allowed()
    {
        var client = api.CreateClient(Guid.NewGuid());
        var patient = await CreatePatientAsync(client);
        await CreateAppointmentAsync(client, patient, Guid.NewGuid(), At(10), 30);

        var other = await PostAppointmentAsync(client, NewAppointment(patient, Guid.NewGuid(), At(10), 30));

        Assert.Equal(HttpStatusCode.Created, other.StatusCode);
    }

    [Theory]
    [InlineData(14, "too_short")]
    [InlineData(241, "too_long")]
    public async Task Create_with_duration_outside_limits_returns_400_validation_failed(int minutes, string expected)
    {
        var client = api.CreateClient(Guid.NewGuid());
        var patient = await CreatePatientAsync(client);

        var response = await PostAppointmentAsync(client, NewAppointment(patient, Guid.NewGuid(), At(10), minutes));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("validation.failed", doc.RootElement.GetProperty("code").GetString());
        Assert.Equal(expected, doc.RootElement.GetProperty("errors").GetProperty("endsAt")[0].GetString());
    }

    [Fact]
    public async Task Create_with_end_before_start_returns_400_validation_failed()
    {
        var client = api.CreateClient(Guid.NewGuid());
        var patient = await CreatePatientAsync(client);

        var response = await PostAppointmentAsync(client, NewAppointment(patient, Guid.NewGuid(), At(10), -30));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("validation.failed", doc.RootElement.GetProperty("code").GetString());
        Assert.Equal("end_before_start", doc.RootElement.GetProperty("errors").GetProperty("endsAt")[0].GetString());
    }

    [Fact]
    public async Task Create_with_start_in_the_past_returns_400_in_past()
    {
        var client = api.CreateClient(Guid.NewGuid());
        var patient = await CreatePatientAsync(client);
        var past = api.Time.GetUtcNow().AddMinutes(-30);

        var response = await PostAppointmentAsync(client, NewAppointment(patient, Guid.NewGuid(), past, 30));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("appointment.in_past", await CodeAsync(response));
    }

    [Fact]
    public async Task Create_with_unknown_patient_returns_404_patient_not_found()
    {
        var client = api.CreateClient(Guid.NewGuid());

        var response = await PostAppointmentAsync(client, NewAppointment(Guid.NewGuid(), Guid.NewGuid(), At(10)));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("patient.not_found", await CodeAsync(response));
    }

    [Fact]
    public async Task Create_with_archived_patient_returns_404_patient_not_found()
    {
        var client = api.CreateClient(Guid.NewGuid());
        var patient = await CreatePatientAsync(client);
        await api.ExecuteSqlAsync($"UPDATE patients SET \"ArchivedAt\" = {api.Time.GetUtcNow()} WHERE \"Id\" = {patient}");

        var response = await PostAppointmentAsync(client, NewAppointment(patient, Guid.NewGuid(), At(10)));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("patient.not_found", await CodeAsync(response));
    }

    [Fact]
    public async Task Cancel_sets_status_and_frees_the_slot()
    {
        var client = api.CreateClient(Guid.NewGuid());
        var patient = await CreatePatientAsync(client);
        var chair = Guid.NewGuid();
        var id = await CreateAppointmentAsync(client, patient, chair, At(10), 30);

        var cancelled = await client.PostAsync($"/appointments/{id}/cancel", content: null);

        Assert.Equal(HttpStatusCode.OK, cancelled.StatusCode);
        using var doc = JsonDocument.Parse(await cancelled.Content.ReadAsStringAsync());
        Assert.Equal("Cancelled", doc.RootElement.GetProperty("status").GetString());

        var reuse = await PostAppointmentAsync(client, NewAppointment(patient, chair, At(10, 15), 30));
        Assert.Equal(HttpStatusCode.Created, reuse.StatusCode);
    }

    [Fact]
    public async Task Cancel_twice_returns_200_idempotently()
    {
        var client = api.CreateClient(Guid.NewGuid());
        var patient = await CreatePatientAsync(client);
        var id = await CreateAppointmentAsync(client, patient, Guid.NewGuid(), At(10));

        var first = await client.PostAsync($"/appointments/{id}/cancel", content: null);
        var second = await client.PostAsync($"/appointments/{id}/cancel", content: null);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(await first.Content.ReadAsStringAsync(), await second.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Cancel_unknown_id_returns_404_appointment_not_found()
    {
        var client = api.CreateClient(Guid.NewGuid());

        var response = await client.PostAsync($"/appointments/{Guid.NewGuid()}/cancel", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("appointment.not_found", await CodeAsync(response));
    }

    [Fact]
    public async Task Tenant_isolation_tenant_b_cannot_see_or_cancel_tenant_a_appointments()
    {
        var tenantA = api.CreateClient(Guid.NewGuid());
        var tenantB = api.CreateClient(Guid.NewGuid());
        var chair = Guid.NewGuid();
        var patientA = await CreatePatientAsync(tenantA);
        var id = await CreateAppointmentAsync(tenantA, patientA, chair, At(10));

        // The same chair and time is free in another tenant.
        var patientB = await CreatePatientAsync(tenantB);
        Assert.Equal(HttpStatusCode.Created, (await PostAppointmentAsync(tenantB, NewAppointment(patientB, chair, At(10)))).StatusCode);

        // Tenant B sees only its own appointment and cannot cancel tenant A's.
        var list = await tenantB.GetFromJsonAsync<JsonElement>($"/appointments?date={_day:yyyy-MM-dd}");
        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal(HttpStatusCode.NotFound, (await tenantB.PostAsync($"/appointments/{id}/cancel", content: null)).StatusCode);
    }

    [Fact]
    public async Task Create_with_same_key_and_same_body_replays_the_stored_response()
    {
        var client = api.CreateClient(Guid.NewGuid());
        var patient = await CreatePatientAsync(client);
        var body = NewAppointment(patient, Guid.NewGuid(), At(10));
        var key = Guid.NewGuid().ToString();

        var first = await PostAppointmentAsync(client, body, key);
        var second = await PostAppointmentAsync(client, body, key);

        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.Equal(await first.Content.ReadAsStringAsync(), await second.Content.ReadAsStringAsync());
        Assert.True(second.Headers.Contains("Idempotency-Replayed"));
        Assert.Equal(1, (await client.GetFromJsonAsync<JsonElement>($"/appointments?date={_day:yyyy-MM-dd}")).GetArrayLength());
    }

    [Fact]
    public async Task Create_with_same_key_and_different_body_returns_409_key_reuse()
    {
        var client = api.CreateClient(Guid.NewGuid());
        var patient = await CreatePatientAsync(client);
        var key = Guid.NewGuid().ToString();
        await PostAppointmentAsync(client, NewAppointment(patient, Guid.NewGuid(), At(10)), key);

        var reuse = await PostAppointmentAsync(client, NewAppointment(patient, Guid.NewGuid(), At(11)), key);

        Assert.Equal(HttpStatusCode.Conflict, reuse.StatusCode);
        Assert.Equal("idempotency.key_reuse", await CodeAsync(reuse));
    }

    [Fact]
    public async Task Create_without_idempotency_key_returns_400()
    {
        var client = api.CreateClient(Guid.NewGuid());
        var patient = await CreatePatientAsync(client);

        var response = await client.PostAsJsonAsync("/appointments", NewAppointment(patient, Guid.NewGuid(), At(10)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("idempotency.key_missing", await CodeAsync(response));
    }

    [Fact]
    public async Task Request_without_tenant_header_returns_400_tenant_missing()
    {
        var client = api.CreateClient(tenantId: null);

        var response = await client.GetAsync($"/appointments?date={_day:yyyy-MM-dd}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("tenant.missing", await CodeAsync(response));
    }
}
