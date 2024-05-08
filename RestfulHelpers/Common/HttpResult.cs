using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
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
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TransactionHelpers;
using TransactionHelpers.Exceptions;
using TransactionHelpers.Interface;

namespace RestfulHelpers.Common;

/// <summary>
/// The base result for all HTTP requests.
/// </summary>
public class HttpResult : Result, IHttpResult
{
    internal HttpStatusCode InternalStatusCode = default;

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
    public override bool Success<TAppend>(TAppend resultAppend)
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
    public override bool Success<TAppend, TAppendValue>(TAppend resultAppend, out TAppendValue? value)
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
    public override bool SuccessAndHasValue<TAppend>(TAppend resultAppend)
    {
        if (resultAppend is IHttpResult httpResult)
        {
            this.WithHttpResult(httpResult);
        }
        else
        {
            this.WithResult(resultAppend);
        }
        if (resultAppend.GetType().GetProperty(nameof(IResult<object>.HasNoValue), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) is PropertyInfo hasNoValuePropertyInfo &&
            hasNoValuePropertyInfo.GetValue(resultAppend) is bool hasNoValue)
        {
            return !resultAppend.IsError && !hasNoValue;
        }
        return !resultAppend.IsError;
    }

    /// <inheritdoc/>
    [MemberNotNullWhen(false, nameof(Error))]
    public override bool SuccessAndHasValue<TAppend, TAppendValue>(TAppend resultAppend, [NotNullWhen(true)] out TAppendValue? value)
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
            if (HttpError is HttpError httpError)
            {
                return httpError.StatusCode;
            }
            return InternalStatusCode;
        }
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
    internal HttpStatusCode InternalStatusCode = default;

    /// <inheritdoc/>
    [JsonIgnore]
    public HttpError? HttpError => Error as HttpError;

    /// <inheritdoc/>
    public HttpStatusCode StatusCode
    {
        get
        {
            if (HttpError is HttpError httpError)
            {
                return httpError.StatusCode;
            }
            return InternalStatusCode;
        }
        init
        {
            InternalStatusCode = value;
        }
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
