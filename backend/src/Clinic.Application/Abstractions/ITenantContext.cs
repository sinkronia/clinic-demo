namespace Clinic.Application.Abstractions;

/// <summary>The tenant (clinic) of the current request.</summary>
public interface ITenantContext
{
    bool HasTenant { get; }

    /// <summary>The current tenant id; <see cref="Guid.Empty"/> when there is no tenant.</summary>
    Guid TenantId { get; }
}
