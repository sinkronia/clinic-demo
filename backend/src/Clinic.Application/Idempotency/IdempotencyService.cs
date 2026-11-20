using Clinic.Domain.Idempotency;

namespace Clinic.Application.Idempotency;

public sealed record StoredResponse(int StatusCode, string Body);

public enum IdempotencyStatus
{
    /// <summary>The action ran now and its response was stored.</summary>
    Executed,

    /// <summary>The key was seen before with the same request; the stored response is returned.</summary>
    Replayed,

    /// <summary>The key was seen before with a different request.</summary>
    KeyReuse,
}

public sealed record IdempotencyOutcome(IdempotencyStatus Status, StoredResponse? Response);

public interface IIdempotencyStore
{
    Task<IdempotencyRecord?> FindAsync(string key, CancellationToken ct);

    Task SaveAsync(IdempotencyRecord record, CancellationToken ct);
}

/// <summary>
/// Makes create operations idempotent. Reusable by any endpoint: pass the key, a hash of the request, and the work to run.
/// Simplification for the demo: the response is stored after the action, not in the same transaction, and two
/// concurrent requests with the same key are not serialized.
/// </summary>
public sealed class IdempotencyService(IIdempotencyStore store, TimeProvider time)
{
    public async Task<IdempotencyOutcome> ExecuteAsync(
        string key,
        string requestHash,
        Func<CancellationToken, Task<StoredResponse>> action,
        CancellationToken ct)
    {
        var existing = await store.FindAsync(key, ct);
        if (existing is not null)
        {
            return existing.RequestHash == requestHash
                ? new IdempotencyOutcome(IdempotencyStatus.Replayed, new StoredResponse(existing.StatusCode, existing.ResponseBody))
                : new IdempotencyOutcome(IdempotencyStatus.KeyReuse, null);
        }

        var response = await action(ct);
        await store.SaveAsync(
            new IdempotencyRecord
            {
                Id = Guid.CreateVersion7(),
                Key = key,
                RequestHash = requestHash,
                StatusCode = response.StatusCode,
                ResponseBody = response.Body,
                CreatedAt = time.GetUtcNow(),
            },
            ct);
        return new IdempotencyOutcome(IdempotencyStatus.Executed, response);
    }
}
