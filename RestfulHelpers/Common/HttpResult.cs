using Microsoft.AspNetCore.Mvc;
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

/// <summary>
/// The base result for all HTTP requests.
/// </summary>
public class HttpResult : Result, IHttpResult
{
    HttpStatusCode IHttpResult.InternalStatusCode { get; set; }

    /// <inheritdoc/>
    [JsonIgnore]
    public override Error? Error => base.Error;

    /// <inheritdoc/>
    [MemberNotNullWhen(false, nameof(Error))]
    public override bool IsSuccess => base.IsSuccess;

    /// <inheritdoc/>
    [MemberNotNullWhen(true, nameof(Error))]
    [JsonIgnore]
    public override bool IsError => base.IsError;

    /// <inheritdoc/>
    [MemberNotNullWhen(false, nameof(Error))]
    public override bool Success<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TAppend>(TAppend resultAppend)
    {
        if (resultAppend is IHttpResult httpResult)
        {
            this.WithHttpResult(httpResult);
        }
        else
        {
            this.WithResult(resultAppend);
        }
        return !resultAppend.IsError;
    }

    /// <inheritdoc/>
    [MemberNotNullWhen(false, nameof(Error))]
    public override bool Success<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TAppend, TAppendValue>(TAppend resultAppend, out TAppendValue? value)
        where TAppendValue : default
    {
        if (resultAppend is IHttpResult httpResult)
        {
            this.WithHttpResult(httpResult);
        }
        else
        {
            this.WithResult(resultAppend);
        }
        value = resultAppend.Value;
        return !resultAppend.IsError;
    }

    /// <inheritdoc/>
    [MemberNotNullWhen(false, nameof(Error))]
    public override bool SuccessAndHasValue<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TAppend>(TAppend resultAppend)
    {
        if (resultAppend is IHttpResult httpResult)
        {
            this.WithHttpResult(httpResult);
        }
        else
        {
            this.WithResult(resultAppend);
        }
        if (typeof(TAppend).GetProperty(nameof(IResult<object>.HasNoValue), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) is PropertyInfo hasNoValuePropertyInfo &&
            hasNoValuePropertyInfo.GetValue(resultAppend) is bool hasNoValue)
        {
            return !resultAppend.IsError && !hasNoValue;
        }
        return !resultAppend.IsError;
    }

    /// <inheritdoc/>
    [MemberNotNullWhen(false, nameof(Error))]
    public override bool SuccessAndHasValue<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TAppend, TAppendValue>(TAppend resultAppend, [NotNullWhen(true)] out TAppendValue? value)
        where TAppendValue : default
    {
        if (resultAppend is IHttpResult httpResult)
        {
            this.WithHttpResult(httpResult);
        }
        else
        {
            this.WithResult(resultAppend);
        }
        value = resultAppend.Value;
        return !resultAppend.IsError && !resultAppend.HasNoValue;
    }

    /// <inheritdoc/>
    [JsonIgnore]
    public HttpError? HttpError => Error as HttpError;

    /// <inheritdoc/>
    public HttpStatusCode StatusCode
    {
        get
        {
            if ((this as IHttpResult).InternalStatusCode == 0)
            {
                if (HttpError is HttpError httpError)
                {
                    return httpError.StatusCode;
                }
                else if (IsError)
                {
                    return HttpStatusCode.InternalServerError;
                }

                return HttpStatusCode.OK;
            }

            return (this as IHttpResult).InternalStatusCode;
        }
        set => this.WithStatusCode(value);
    }

    /// <inheritdoc/>
    public Task ExecuteResultAsync(ActionContext context)
    {
        var actionResult = new ObjectResult(this)
        {
            DeclaredType = GetType(),
            StatusCode = (int)StatusCode
        };
        return actionResult.ExecuteResultAsync(context);
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

    Task IActionResult.ExecuteResultAsync(ActionContext context)
    {
        throw new NotImplementedException();
    }
#endif

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

    /// <inheritdoc/>
    [JsonIgnore]
    public HttpError? HttpError => Error as HttpError;

    /// <inheritdoc/>
    public HttpStatusCode StatusCode
    {
        get
        {
            if ((this as IHttpResult).InternalStatusCode == 0)
            {
                if (HttpError is HttpError httpError)
                {
                    return httpError.StatusCode;
                }
                else if (IsError)
                {
                    return HttpStatusCode.InternalServerError;
                }

                return HttpStatusCode.OK;
            }

            return (this as IHttpResult).InternalStatusCode;
        }
        set => this.WithStatusCode(value);
    }

    /// <inheritdoc/>
    public Task ExecuteResultAsync(ActionContext context)
    {
        var actionResult = new ObjectResult(this)
        {
            DeclaredType = GetType(),
            StatusCode = (int)StatusCode
        };
        return actionResult.ExecuteResultAsync(context);
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
