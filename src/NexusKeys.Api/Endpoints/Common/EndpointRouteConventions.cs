using System.Reflection;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace GameStore.API.Endpoints;

internal static class EndpointRouteConventions
{
    public static bool IsV1(string apiPrefix) =>
        string.IsNullOrEmpty(apiPrefix)
        || apiPrefix.EndsWith("/v1", StringComparison.OrdinalIgnoreCase);

    public static string PublicPrefix(string apiPrefix) =>
        string.IsNullOrEmpty(apiPrefix) ? "/api/v1" : apiPrefix;
}

public sealed class V1ProblemDetailsFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var result = await next(context);
        if (result is not IStatusCodeHttpResult statusResult
            || statusResult.StatusCode is not int statusCode
            || statusCode < StatusCodes.Status400BadRequest)
        {
            return result;
        }

        if (result is IValueHttpResult { Value: ProblemDetails })
        {
            return result;
        }

        var detail = result is IValueHttpResult valueResult
            ? ExtractDetail(valueResult.Value)
            : null;

        return Results.Problem(
            statusCode: statusCode,
            title: ReasonPhrases.GetReasonPhrase(statusCode),
            detail: detail);
    }

    private static string? ExtractDetail(object? value)
    {
        if (value is null) return null;
        if (value is string text) return text;

        var messageProperty = value.GetType().GetProperty(
            "message",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        return messageProperty?.GetValue(value)?.ToString();
    }
}
