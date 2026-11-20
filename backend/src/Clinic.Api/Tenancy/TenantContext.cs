using Clinic.Application.Abstractions;

namespace Clinic.Api.Tenancy;

internal sealed class TenantContext : ITenantContext
{
    public bool HasTenant => TenantId != Guid.Empty;

    public Guid TenantId { get; set; }
}
