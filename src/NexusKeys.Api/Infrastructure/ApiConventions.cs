using Microsoft.AspNetCore.Mvc;

namespace GameStore.API.Infrastructure;

public sealed class PaginationQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    public (int Page, int PageSize) Normalize(int maximumPageSize = 100)
    {
        return (Math.Max(1, Page), Math.Clamp(PageSize, 1, maximumPageSize));
    }
}

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Data,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);

public static class ApiProblems
{
    public static IResult BadRequest(string detail, string? type = null) =>
        Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Bad request",
            detail: detail,
            type: type);

    public static IResult Unauthorized(string detail = "Authentication is required.") =>
        Results.Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Unauthorized",
            detail: detail);

    public static IResult Forbidden(string detail = "You do not have permission to perform this action.") =>
        Results.Problem(
            statusCode: StatusCodes.Status403Forbidden,
            title: "Forbidden",
            detail: detail);

    public static IResult NotFound(string detail, string? type = null) =>
        Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Resource not found",
            detail: detail,
            type: type);

    public static IResult Conflict(string detail, string? type = null) =>
        Results.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Resource conflict",
            detail: detail,
            type: type);

    public static IResult UnprocessableEntity(string detail, string? type = null) =>
        Results.Problem(
            statusCode: StatusCodes.Status422UnprocessableEntity,
            title: "Validation failed",
            detail: detail,
            type: type);
}

public sealed class LegacyApiDeprecationMiddleware(RequestDelegate next)
{
    private static readonly PathString V1Prefix = new("/api/v1");
    private static readonly PathString ApiPrefix = new("/api");
    private static readonly PathString IntegrationPrefix = new("/api/integrations");
    private static readonly PathString WebhookPrefix = new("/api/webhooks");

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path;
        var isLegacyApi = path.StartsWithSegments(ApiPrefix)
            && !path.StartsWithSegments(V1Prefix)
            && !path.StartsWithSegments(IntegrationPrefix)
            && !path.StartsWithSegments(WebhookPrefix);

        if (isLegacyApi)
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers.TryAdd("Deprecation", "true");
                context.Response.Headers.TryAdd("Sunset", "Fri, 31 Dec 2027 23:59:59 GMT");
                context.Response.Headers.TryAdd("Link", "</api/v1>; rel=\"successor-version\"");
                return Task.CompletedTask;
            });
        }

        await next(context);
    }
}
