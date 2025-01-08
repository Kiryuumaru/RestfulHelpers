using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
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

internal static class HttpResultCommon
{
    public static void Append(IHttpResult httpResult, ResultAppend resultAppend)
    {
        if (resultAppend is HttpResultAppend httpResultAppend)
        {
            if (httpResultAppend.ShouldAppendStatusCode || httpResultAppend.ShouldAppendStatusCodeOrError)
            {
                httpResult.InternalStatusCode = httpResultAppend.StatusCode;
            }
            if (httpResultAppend.ShouldAppendStatusCodeOrError)
            {
                if ((int)httpResultAppend.StatusCode < 200 || (int)httpResultAppend.StatusCode > 299)
                {
                    List<Error> errors = [];
                    if (resultAppend.Errors != null)
                    {
                        foreach (var error in resultAppend.Errors)
                        {
                            if (error != null)
                            {
                                errors.Add(error);
                            }
                        }
                    }
                    var httpError = new HttpError();
                    httpError.SetStatusCode(httpResultAppend.StatusCode, null);
                    errors.Add(httpError);
                    resultAppend.Errors = errors.AsReadOnly();
                    httpResultAppend.ShouldAppendErrors = true;
                }
            }
            if (httpResultAppend.ShouldAppendHeaders)
            {
                if (httpResultAppend.ResponseHeaders != null)
                {
                    foreach (var header in httpResultAppend.ResponseHeaders)
                    {
                        var headerName = header.Key;
                        var headerValues = header.Value;
                        if (httpResult.InternalResponseHeaders.TryGetValue(headerName, out string[]? value))
                        {
                            var existingValues = value.ToList();
                            existingValues.AddRange(headerValues);
                            httpResult.InternalResponseHeaders[headerName] = [.. existingValues];
                        }
                        else
                        {
                            httpResult.InternalResponseHeaders[headerName] = headerValues;
                        }
                    }
                }
            }
            if (httpResultAppend.ShouldReplaceHeaders)
            {
                if (httpResultAppend.ResponseHeaders != null)
                {
                    httpResult.InternalResponseHeaders = httpResultAppend.ResponseHeaders.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                }
            }
        }

        if (resultAppend.ShouldAppendErrors)
        {
            if (resultAppend.Errors != null)
            {
                foreach (var error in resultAppend.Errors)
                {
                    if (error is HttpError httpError)
                    {
                        httpResult.Append(new HttpResultAppend()
                        {
                            StatusCode = httpError.StatusCode,
                            ShouldAppendStatusCode = true
                        });
                    }
                    else
                    {
                        httpResult.Append(new HttpResultAppend()
                        {
                            StatusCode = HttpStatusCode.InternalServerError,
                            ShouldAppendStatusCode = true
                        });
                    }
                }
            }
        }
        if (resultAppend.ShouldAppendResultValue || resultAppend.ShouldAppendResultErrors || resultAppend.ShouldReplaceResultErrors)
        {
            if (resultAppend.Results != null)
            {
                foreach (var result in resultAppend.Results)
                {
                    if (result is IHttpResult httpResultToAppend)
                    {
                        httpResult.Append(new HttpResultAppend() { StatusCode = httpResultToAppend.StatusCode, ShouldAppendStatusCode = true });
                        httpResult.Append(new HttpResultAppend() { ResponseHeaders = httpResultToAppend.ResponseHeaders, ShouldReplaceHeaders = true });
                    }
                }
            }
        }
    }

    public static void HandleClone(IHttpResult httpResult, object clone)
    {
        if (clone is IHttpResult httpResultClone)
        {
            httpResultClone.InternalStatusCode = httpResult.InternalStatusCode;
            httpResultClone.InternalResponseHeaders = httpResult.InternalResponseHeaders.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }

    public static Task ExecuteResultAsync<THttpResult>(THttpResult httpResult, ActionContext context)
        where THttpResult : IHttpResult
    {
        var actionResult = new ObjectResult(httpResult)
        {
            DeclaredType = typeof(THttpResult),
            StatusCode = (int)httpResult.StatusCode
        };
        return actionResult.ExecuteResultAsync(context);
    }
}

/// <summary>
/// The base result for all HTTP requests.
/// </summary>
public class HttpResult : Result, IHttpResult
{
    HttpStatusCode IHttpResult.InternalStatusCode { get; set; } = HttpStatusCode.OK;

    Dictionary<string, string[]> IHttpResult.InternalResponseHeaders { get; set; } = [];

    /// <inheritdoc/>
    [JsonIgnore]
    public HttpError? HttpError => Error as HttpError;

    /// <inheritdoc/>
    public HttpStatusCode StatusCode
    {
        get => (this as IHttpResult).InternalStatusCode;
        set => Append(new HttpResultAppend() { StatusCode = value, ShouldAppendStatusCodeOrError = true });
    }

    /// <inheritdoc/>
    [JsonIgnore]
    public IReadOnlyDictionary<string, string[]> ResponseHeaders
    {
        get => (this as IHttpResult).InternalResponseHeaders.AsReadOnly();
        set => Append(new HttpResultAppend() { ResponseHeaders = value, ShouldReplaceHeaders = true });
    }

    /// <inheritdoc/>
    public Task ExecuteResultAsync(ActionContext context)
    {
        return HttpResultCommon.ExecuteResultAsync(this, context);
    }

#if NET7_0_OR_GREATER
    /// <summary>
    /// Creates the corresponding <see cref="IHttpResultResponse"/>.
    /// </summary>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use when deserializing the result.</param>
    /// <returns></returns>
    public IHttpResultResponse GetResponse(JsonSerializerOptions? jsonSerializerOptions = null)
    {
        return HttpResultResponse.Create(this, jsonSerializerOptions);
    }
#endif

    /// <inheritdoc/>
    public override void Append(ResultAppend resultAppend)
    {
        HttpResultCommon.Append(this, resultAppend);
        base.Append(resultAppend);
    }

    /// <inheritdoc/>
    public override object Clone()
    {
        HttpResult httpResult = new();
        HandleClone(httpResult);
        return httpResult;
    }

    /// <inheritdoc/>
    protected override void HandleClone(object clone)
    {
        base.HandleClone(clone);
        HttpResultCommon.HandleClone(this, clone);
    }

    /// <summary>
    /// Implicit operator for <see cref="Error"/> conversion.
    /// </summary>
    /// <param name="error">
    /// The <see cref="Error"/> to return.
    /// </param>
    public static implicit operator HttpResult(Error? error)
    {
        return new HttpResult().WithError(error);
    }

    /// <summary>
    /// Implicit operator for <see cref="Exception"/> conversion.
    /// </summary>
    /// <param name="exception">
    /// The <see cref="Exception"/> to return.
    /// </param>
    public static implicit operator HttpResult(Exception? exception)
    {
        return new HttpResult().WithError(exception);
    }

    /// <summary>
    /// Implicit operator for <see cref="HttpStatusCode"/> conversion.
    /// </summary>
    /// <param name="httpStatusCode">
    /// The <see cref="HttpStatusCode"/> to return.
    /// </param>
    public static implicit operator HttpResult(HttpStatusCode httpStatusCode)
    {
        return new HttpResult().WithStatusCode(httpStatusCode);
    }

    /// <summary>
    /// Implicit operator for <see cref="HttpResult"/> to <see cref="Error"/> conversion.
    /// </summary>
    /// <param name="result">
    /// The <see cref="Result"/> to convert.
    /// </param>
    public static implicit operator Error?(HttpResult result)
    {
        return result.Error;
    }

    /// <summary>
    /// Implicit operator for <see cref="HttpResult"/> to <see cref="Exception"/> conversion.
    /// </summary>
    /// <param name="result">
    /// The <see cref="Result{TValue}"/> to convert.
    /// </param>
    public static implicit operator Exception?(HttpResult result)
    {
        return result.Error?.Exception;
    }

    /// <summary>
    /// Implicit operator for <see cref="HttpResult"/> to <see cref="Common.HttpError"/> conversion.
    /// </summary>
    /// <param name="result">
    /// The <see cref="Result"/> to convert.
    /// </param>
    public static implicit operator HttpError?(HttpResult result)
    {
        return result.HttpError;
    }

    /// <summary>
    /// Implicit operator for <see cref="HttpResult"/> to <see cref="HttpStatusCode"/> conversion.
    /// </summary>
    /// <param name="result">
    /// The <see cref="Result"/> to convert.
    /// </param>
    public static implicit operator HttpStatusCode?(HttpResult result)
    {
        return result.StatusCode;
    }

    /// <summary>
    /// Implicit operator for <see cref="HttpResult"/> to <see cref="ActionResult{HttpResult}"/> conversion.
    /// </summary>
    /// <param name="result">
    /// The <see cref="Result{TValue}"/> to convert.
    /// </param>
    public static implicit operator ActionResult<HttpResult>(HttpResult result)
    {
        return result.ToActionResult();
    }
}

/// <summary>
/// The base result for all HTTP requests.
/// </summary>
/// <inheritdoc/>
public class HttpResult<TValue> : Result<TValue>, IHttpResult<TValue>
{
    HttpStatusCode IHttpResult.InternalStatusCode { get; set; }

    Dictionary<string, string[]> IHttpResult.InternalResponseHeaders { get; set; } = [];

    /// <inheritdoc/>
    [JsonIgnore]
    public HttpError? HttpError => Error as HttpError;

    /// <inheritdoc/>
    public HttpStatusCode StatusCode
    {
        get => (this as IHttpResult).InternalStatusCode;
        set => Append(new HttpResultAppend() { StatusCode = value, ShouldAppendStatusCodeOrError = true });
    }

    /// <inheritdoc/>
    [JsonIgnore]
    public IReadOnlyDictionary<string, string[]> ResponseHeaders
    {
        get => (this as IHttpResult).InternalResponseHeaders.AsReadOnly();
        set => Append(new HttpResultAppend() { ResponseHeaders = value, ShouldReplaceHeaders = true });
    }

    /// <inheritdoc/>
    public Task ExecuteResultAsync(ActionContext context)
    {
        return HttpResultCommon.ExecuteResultAsync(this, context);
    }

#if NET7_0_OR_GREATER
    /// <summary>
    /// Creates the corresponding <see cref="IHttpResultResponse"/>.
    /// </summary>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use when deserializing the result.</param>
    /// <returns></returns>
    [RequiresDynamicCode(RequiresDynamicCode)]
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    public IHttpResultResponse<TValue> GetResponse(JsonSerializerOptions? jsonSerializerOptions = null)
    {
        return HttpResultResponse<TValue>.Create(this, jsonSerializerOptions);
    }

    /// <summary>
    /// Creates the corresponding <see cref="IHttpResultResponse"/>.
    /// </summary>
    /// <param name="jsonTypeInfo">The <see cref="JsonTypeInfo"/> metadata to use when deserializing the result.</param>
    /// <returns></returns>
    public IHttpResultResponse<TValue> GetResponse(JsonTypeInfo<TValue> jsonTypeInfo)
    {
        return HttpResultResponse<TValue>.Create(this, jsonTypeInfo);
    }
#endif

    /// <inheritdoc/>
    public override void Append(ResultAppend resultAppend)
    {
        HttpResultCommon.Append(this, resultAppend);
        base.Append(resultAppend);
    }

    /// <inheritdoc/>
    public override object Clone()
    {
        HttpResult<TValue> httpResult = new();
        HandleClone(httpResult);
        return httpResult;
    }

    /// <inheritdoc/>
    protected override void HandleClone(object clone)
    {
        base.HandleClone(clone);
        HttpResultCommon.HandleClone(this, clone);
    }

    /// <summary>
    /// Implicit operator for <see cref="Error"/> conversion.
    /// </summary>
    /// <param name="error">
    /// The <see cref="Error"/> to return.
    /// </param>
    public static implicit operator HttpResult<TValue>(Error? error)
    {
        return new HttpResult<TValue>().WithError(error);
    }

    /// <summary>
    /// Implicit operator for <see cref="Exception"/> conversion.
    /// </summary>
    /// <param name="exception">
    /// The <see cref="Exception"/> to return.
    /// </param>
    public static implicit operator HttpResult<TValue>(Exception? exception)
    {
        return new HttpResult<TValue>().WithError(exception);
    }

    /// <summary>
    /// Implicit operator for <see cref="HttpStatusCode"/> conversion.
    /// </summary>
    /// <param name="httpStatusCode">
    /// The <see cref="HttpStatusCode"/> to return.
    /// </param>
    public static implicit operator HttpResult<TValue>(HttpStatusCode httpStatusCode)
    {
        return new HttpResult<TValue>().WithStatusCode(httpStatusCode);
    }

    /// <summary>
    /// Implicit operator for <see cref="HttpResult{TValue}"/> to <see cref="Error"/> conversion.
    /// </summary>
    /// <param name="result">
    /// The <see cref="Result"/> to convert.
    /// </param>
    public static implicit operator Error?(HttpResult<TValue> result)
    {
        return result.Error;
    }

    /// <summary>
    /// Implicit operator for <see cref="HttpResult{TValue}"/> to <see cref="Exception"/> conversion.
    /// </summary>
    /// <param name="result">
    /// The <see cref="Result{TValue}"/> to convert.
    /// </param>
    public static implicit operator Exception?(HttpResult<TValue> result)
    {
        return result.Error?.Exception;
    }

    /// <summary>
    /// Implicit operator for <see cref="HttpResult{TValue}"/> to <see cref="Common.HttpError"/> conversion.
    /// </summary>
    /// <param name="result">
    /// The <see cref="Result"/> to convert.
    /// </param>
    public static implicit operator HttpResult<TValue>?(HttpResult result)
    {
        return result.HttpError;
    }

    /// <summary>
    /// Implicit operator for <see cref="HttpResult{TValue}"/> to <see cref="HttpStatusCode"/> conversion.
    /// </summary>
    /// <param name="result">
    /// The <see cref="Result"/> to convert.
    /// </param>
    public static implicit operator HttpStatusCode?(HttpResult<TValue> result)
    {
        return result.StatusCode;
    }

    /// <summary>
    /// Implicit operator for <see cref="Error"/> conversion.
    /// </summary>
    /// <param name="value">
    /// The <typeparamref name="TValue"/> to return.
    /// </param>
    public static implicit operator HttpResult<TValue>(TValue? value)
    {
        return new HttpResult<TValue>().WithValue(value);
    }

    /// <summary>
    /// Implicit operator for <see cref="HttpResult{TValue}"/> to <typeparamref name="TValue"/> conversion.
    /// </summary>
    /// <param name="result">
    /// The <see cref="Result{TValue}"/> to convert.
    /// </param>
    public static implicit operator TValue?(HttpResult<TValue> result)
    {
        return result.Value;
    }

    /// <summary>
    /// Implicit operator for <see cref="HttpResult{TValue}"/> to <see cref="ActionResult{HttpResult}"/> conversion.
    /// </summary>
    /// <param name="result">
    /// The <see cref="Result{TValue}"/> to convert.
    /// </param>
    public static implicit operator ActionResult<HttpResult<TValue>>(HttpResult<TValue> result)
    {
        return result.ToActionResult();
    }
}
