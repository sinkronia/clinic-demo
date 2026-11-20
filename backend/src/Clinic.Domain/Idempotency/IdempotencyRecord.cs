namespace Clinic.Domain.Idempotency;

/// <summary>Stored response of a create request, keyed by tenant and Idempotency-Key. Reusable by any endpoint.</summary>
public sealed class IdempotencyRecord : ITenantEntity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public required string Key { get; set; }

    public required string RequestHash { get; set; }

    public int StatusCode { get; set; }

    public required string ResponseBody { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
