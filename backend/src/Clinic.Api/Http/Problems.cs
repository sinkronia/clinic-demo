using System.Text.Json;
using Clinic.Application.Results;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Api.Http;

/// <summary>Builds RFC 9457 ProblemDetails with a stable `code` extension (BE-009).</summary>
internal static class Problems
{
    public static int StatusFor(ErrorKind kind) => kind switch
    {
        ErrorKind.Validation => StatusCodes.Status400BadRequest,
        ErrorKind.NotFound => StatusCodes.Status404NotFound,
        ErrorKind.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError,
    };

    public static ProblemDetails Create(int status, string code, IReadOnlyDictionary<string, string[]>? fieldErrors = null)
    {
        var problem = new ProblemDetails { Status = status, Title = code };
        problem.Extensions["code"] = code;
        if (fieldErrors is { Count: > 0 })
        {
            problem.Extensions["errors"] = fieldErrors;
        }

        return problem;
    }

    public static ProblemDetails From(BusinessError error) => Create(StatusFor(error.Kind), error.Code, error.FieldErrors);

    public static Task WriteAsync(HttpContext context, int status, string code)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(Create(status, code), JsonSerializerOptions.Web), context.RequestAborted);
    }
}
