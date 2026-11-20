using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Clinic.IntegrationTests;

/// <summary>One real PostgreSQL container and one API host for the whole test collection (BE-012).</summary>
public sealed class ApiFixture : IAsyncLifetime, IDisposable
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18").Build();
    private ApiFactory? _factory;

    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero));

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _factory = new ApiFactory(_postgres.GetConnectionString(), Time);
    }

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    public void Dispose() => _factory?.Dispose();

    /// <summary>A client for the given tenant; each test uses a fresh tenant id, so no cleanup is needed.</summary>
    public HttpClient CreateClient(Guid? tenantId)
    {
        var client = _factory!.CreateClient();
        if (tenantId is not null)
        {
            client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId.Value.ToString());
        }

        return client;
    }

    /// <summary>There is no archive endpoint yet, so tests archive a patient directly in the database.</summary>
    public async Task ArchivePatientAsync(Guid patientId)
    {
        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("UPDATE patients SET \"ArchivedAt\" = now() WHERE \"Id\" = @id", connection);
        command.Parameters.AddWithValue("id", patientId);
        await command.ExecuteNonQueryAsync();
    }

    private sealed class ApiFactory(string connectionString, FakeTimeProvider time) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:Clinic", connectionString);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(time);
            });
        }
    }
}

[CollectionDefinition("api")]
public sealed class ApiTestGroup : ICollectionFixture<ApiFixture>;
