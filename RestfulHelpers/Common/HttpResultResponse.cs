using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RestfulHelpers;
using RestfulHelpers.Common;
using RestfulHelpers.Common.Internals;
using RestfulHelpers.Interface;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using TransactionHelpers;
using TransactionHelpers.Exceptions;
using TransactionHelpers.Interface;

using static RestfulHelpers.Common.Internals.Message;

namespace RestfulHelpers.Common;

#if NET7_0_OR_GREATER

internal static class HttpResultResponseCommon
{
    public static void PatchProblemDetails(IHttpResult httpResult)
    {
        foreach (var error in httpResult.Errors)
        {
            if (error is HttpError httpError)
            {
                if (httpError.Detail is ProblemDetails mvcProblemDetails)
                {
                    var patchProblemDetails = new PatchForProblemDetails()
                    {
                        Status = mvcProblemDetails.Status,
                        Title = mvcProblemDetails.Title,
                        Type = mvcProblemDetails.Type,
                        Detail = mvcProblemDetails.Detail,
                        Instance = mvcProblemDetails.Instance
                    };
                    patchProblemDetails.Extensions.Clear();
                    foreach (var extension in mvcProblemDetails.Extensions)
                    {
                        patchProblemDetails.Extensions.Add(extension);
                    }
                    httpError.Detail = patchProblemDetails;
                }
            }
        }
    }
}

internal class HttpResultResponse : IHttpResultResponse
{
    public static HttpResultResponse Create(HttpResult httpResult, JsonSerializerOptions? jsonSerializerOptions)
    {
        RestfulHelpersJsonSerializerContext context = RestfulHelpersJsonSerializerContext.WebDefault;
        if (jsonSerializerOptions != null)
        {
            context = new RestfulHelpersJsonSerializerContext(jsonSerializerOptions);
        }
        jsonSerializerOptions ??= context.Options;
        return new()
        {
            HttpResult = httpResult,
            JsonSerializerOptions = jsonSerializerOptions,
            RestfulHelpersJsonSerializerContext = context
        };
    }

    public required HttpResult HttpResult { get; init; }

    public required JsonSerializerOptions JsonSerializerOptions { get; init; }

    public required RestfulHelpersJsonSerializerContext RestfulHelpersJsonSerializerContext { get; init; }

    public int? StatusCode => (int)HttpResult.StatusCode;

    public object? Value => HttpResult;

    public Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        httpContext.Response.StatusCode = StatusCode ?? 0;

        foreach (var header in (HttpResult as IHttpResult).InternalResponseHeaders)
        {
            httpContext.Response.Headers.Append(header.Key, header.Value);
        }

        var httpResultClone = (HttpResult.Clone() as HttpResult)!;

        HttpResultResponseCommon.PatchProblemDetails(httpResultClone);

        return httpContext.Response.WriteAsJsonAsync(httpResultClone, RestfulHelpersJsonSerializerContext.HttpResult);
    }

    public Task ExecuteResultAsync(ActionContext context)
    {
        return HttpResult.ExecuteResultAsync(context);
    }
}

internal class HttpResultResponse<T> : IHttpResultResponse<T>
{
    [RequiresDynamicCode(RequiresDynamicCode)]
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    public static HttpResultResponse<T> Create(HttpResult<T> httpResult, JsonSerializerOptions? jsonSerializerOptions)
    {
        RestfulHelpersJsonSerializerContext context = RestfulHelpersJsonSerializerContext.WebDefault;
        if (jsonSerializerOptions != null)
        {
            context = new RestfulHelpersJsonSerializerContext(jsonSerializerOptions);
        }
        jsonSerializerOptions ??= context.Options;
        return new()
        {
            HttpResult = httpResult,
            JsonSerializerOptions = jsonSerializerOptions,
            RestfulHelpersJsonSerializerContext = context,
            JsonTypeInfo = null
        };
    }

    public static HttpResultResponse<T> Create(HttpResult<T> httpResult, JsonTypeInfo<T> jsonTypeInfo)
    {
        return new()
        {
            HttpResult = httpResult,
            JsonSerializerOptions = jsonTypeInfo.Options,
            RestfulHelpersJsonSerializerContext = new RestfulHelpersJsonSerializerContext(jsonTypeInfo.Options),
            JsonTypeInfo = jsonTypeInfo
        };
    }

    public required HttpResult<T> HttpResult { get; init; }

    public required JsonSerializerOptions JsonSerializerOptions { get; init; }

    public required RestfulHelpersJsonSerializerContext RestfulHelpersJsonSerializerContext { get; init; }

    public required JsonTypeInfo<T>? JsonTypeInfo { get; init; }

    public int? StatusCode => throw new NotImplementedException();

    public object? Value => HttpResult;

    HttpResult<T>? IValueHttpResult<HttpResult<T>>.Value => HttpResult;

    public Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        httpContext.Response.StatusCode = StatusCode ?? 0;

        foreach (var header in (HttpResult as IHttpResult).InternalResponseHeaders)
        {
            httpContext.Response.Headers.Append(header.Key, header.Value);
        }

        if (JsonTypeInfo == null)
        {
#pragma warning disable IL2026
#pragma warning disable IL3050
            return httpContext.Response.WriteAsJsonAsync(HttpResult, JsonSerializerOptions);
#pragma warning restore IL2026
#pragma warning restore IL3050
        }
        else
        {
            using var jsonWriter = new Utf8JsonWriter(httpContext.Response.BodyWriter);

            var httpResultClone = (HttpResult.Clone() as IHttpResult)!;

            HttpResultResponseCommon.PatchProblemDetails(httpResultClone);

            var jsonNode = JsonSerializer.SerializeToNode((HttpResult.Clone() as IHttpResult)!, RestfulHelpersJsonSerializerContext.IHttpResult)!;

            string valuePropName = JsonTypeInfo.Options.PropertyNamingPolicy?.ConvertName("Value") ?? "value";

            jsonNode[valuePropName] = JsonSerializer.SerializeToNode(HttpResult.Value!, JsonTypeInfo);

            jsonNode.WriteTo(jsonWriter, JsonSerializerOptions);

            return jsonWriter.FlushAsync();
        }
    }

    public Task ExecuteResultAsync(ActionContext context)
    {
        return HttpResult.ExecuteResultAsync(context);
    }
}
#endif
