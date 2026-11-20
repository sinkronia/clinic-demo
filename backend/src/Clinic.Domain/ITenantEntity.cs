namespace Clinic.Domain;

/// <summary>Marks a business entity that belongs to one tenant (clinic). See rule BE-005.</summary>
public interface ITenantEntity
{
    Guid TenantId { get; set; }
}
