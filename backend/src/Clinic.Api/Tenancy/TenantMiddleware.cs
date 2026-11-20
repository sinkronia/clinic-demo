using Clinic.Api.Http;

namespace Clinic.Api.Tenancy;

/// <summary>
/// Resolves the tenant from the X-Tenant-Id header. DEMO ONLY: a real deployment will read a tenant claim from the
/// authenticated user's JWT instead of trusting a client-supplied header.
/// </summary>
internal sealed class TenantMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Tenant-Id";

    public async Task InvokeAsync(HttpContext context, TenantContext tenant)
    {
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(HeaderName, out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            await Problems.WriteAsync(context, StatusCodes.Status400BadRequest, "tenant.missing");
            return;
        }

        if (!Guid.TryParse(raw, out var tenantId) || tenantId == Guid.Empty)
        {
            await Problems.WriteAsync(context, StatusCodes.Status400BadRequest, "tenant.invalid");
            return;
        }

        tenant.TenantId = tenantId;
        await next(context);
    }
}
