using Microsoft.AspNetCore.Mvc;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Serialization.Json;
using System.Text.Json;

namespace Bff.Api;

internal static class DownstreamResults
{
    public static async Task<IActionResult> OkMappedAsync<TSource, TWire>(
        Func<Task<TSource?>> action,
        Func<TSource, TWire> map)
    {
        try
        {
            var result = await action();
            if (result is null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(map(result));
        }
        catch (ApiException ex)
        {
            return ToProblemResult(ex);
        }
    }

    public static async Task<IActionResult> OkMappedListAsync<TSource, TWire>(
        Func<Task<List<TSource>?>> action,
        Func<TSource, TWire> map)
    {
        try
        {
            var result = await action();
            var mapped = result?.Select(map).ToList() ?? [];
            return new OkObjectResult(mapped);
        }
        catch (ApiException ex)
        {
            return ToProblemResult(ex);
        }
    }

    public static async Task<IActionResult> CreatedMappedAsync<TSource, TWire>(
        Func<Task<TSource?>> action,
        Func<TSource, string> locationFactory,
        Func<TSource, TWire> map)
    {
        try
        {
            var result = await action();
            if (result is null)
            {
                return new StatusCodeResult(StatusCodes.Status201Created);
            }

            return new CreatedResult(locationFactory(result), map(result));
        }
        catch (ApiException ex)
        {
            return ToProblemResult(ex);
        }
    }

    public static async Task<IActionResult> OkAsync<T>(Func<Task<T?>> action)
    {
        try
        {
            var result = await action();
            return new OkObjectResult(result);
        }
        catch (ApiException ex)
        {
            return ToProblemResult(ex);
        }
    }

    public static async Task<IActionResult> CreatedAsync<T>(
        Func<Task<T?>> action,
        Func<T, string> locationFactory)
    {
        try
        {
            var result = await action();
            if (result is null)
            {
                return new StatusCodeResult(StatusCodes.Status201Created);
            }

            return new CreatedResult(locationFactory(result), result);
        }
        catch (ApiException ex)
        {
            return ToProblemResult(ex);
        }
    }

    private static IActionResult ToProblemResult(ApiException ex)
    {
        if (ex is IParsable parsable)
        {
            var content = SerializeProblemDetails(ex, parsable);

            return new ContentResult
            {
                StatusCode = ex.ResponseStatusCode,
                Content = content,
                ContentType = "application/problem+json",
            };
        }

        return new StatusCodeResult(ex.ResponseStatusCode);
    }

    private static string SerializeProblemDetails(ApiException ex, IParsable parsable)
    {
        try
        {
            using var writer = new JsonSerializationWriter();
            writer.WriteObjectValue(null, parsable);
            using var stream = writer.GetSerializedContent();
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        catch (InvalidOperationException)
        {
            var problem = new Dictionary<string, object?>
            {
                ["title"] = GetStringProperty(parsable, "Title") ?? ex.Message,
                ["status"] = ex.ResponseStatusCode,
                ["detail"] = GetStringProperty(parsable, "Detail") ?? ex.Message,
                ["type"] = GetStringProperty(parsable, "Type"),
                ["instance"] = GetStringProperty(parsable, "Instance"),
            };

            return JsonSerializer.Serialize(
                problem
                    .Where(item => item.Value is not null)
                    .ToDictionary(item => item.Key, item => item.Value));
        }
    }

    private static string? GetStringProperty(IParsable parsable, string propertyName) =>
        parsable.GetType().GetProperty(propertyName)?.GetValue(parsable) as string;
}
