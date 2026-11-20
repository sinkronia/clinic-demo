using Clinic.Api.Patients;
using Clinic.Api.Tenancy;
using Clinic.Application;
using Clinic.Application.Abstractions;
using Clinic.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

await app.Services.MigrateDatabaseAsync();

app.UseMiddleware<TenantMiddleware>();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapPatientEndpoints();

await app.RunAsync();

// Needed by WebApplicationFactory in the integration tests.
public partial class Program;
