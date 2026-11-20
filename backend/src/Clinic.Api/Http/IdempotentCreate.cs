using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Clinic.Application.Idempotency;
using Clinic.Application.Results;

namespace Clinic.Api.Http;

/// <summary>Runs a create operation under the Idempotency-Key header (BE-010). Reusable by any create endpoint.</summary>
internal static class IdempotentCreate
{
    public const string HeaderName = "Idempotency-Key";

    public static async Task<IResult> RunAsync<TRequest, TResponse>(
        HttpContext http,
        IdempotencyService idempotency,
        TRequest request,
        Func<CancellationToken, Task<Result<TResponse>>> operation)
    {
        var key = http.Request.Headers[HeaderName].ToString();
        if (string.IsNullOrWhiteSpace(key))
        {
            return Json(StatusCodes.Status400BadRequest, JsonSerializer.Serialize(Problems.Create(StatusCodes.Status400BadRequest, "idempotency.key_missing"), JsonSerializerOptions.Web));
        }

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request, JsonSerializerOptions.Web))));

        var outcome = await idempotency.ExecuteAsync(
            key.Trim(),
            hash,
            async ct =>
            {
                var result = await operation(ct);
                return result.IsSuccess
                    ? new StoredResponse(StatusCodes.Status201Created, JsonSerializer.Serialize(result.Value, JsonSerializerOptions.Web))
                    : new StoredResponse(Problems.StatusFor(result.Error!.Kind), JsonSerializer.Serialize(Problems.From(result.Error), JsonSerializerOptions.Web));
            },
            http.RequestAborted);

        if (outcome.Status == IdempotencyStatus.KeyReuse)
        {
            return Json(StatusCodes.Status409Conflict, JsonSerializer.Serialize(Problems.Create(StatusCodes.Status409Conflict, "idempotency.key_reuse"), JsonSerializerOptions.Web));
        }

        if (outcome.Status == IdempotencyStatus.Replayed)
        {
            http.Response.Headers["Idempotency-Replayed"] = "true";
        }

        return Json(outcome.Response!.StatusCode, outcome.Response.Body);
    }

    private static IResult Json(int status, string body) =>
        Results.Text(body, status >= 400 ? "application/problem+json" : "application/json", Encoding.UTF8, status);
}
