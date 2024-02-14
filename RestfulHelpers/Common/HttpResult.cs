using RestfulHelpers;
using RestfulHelpers.Common;
using RestfulHelpers.Common.Internals;
using RestfulHelpers.Interface;
using System;
using System.Collections.Generic;
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
    private HttpStatusCode statusCode;

    /// <inheritdoc/>
    public HttpError? HttpError
    {
        get => Error as HttpError;
        init => Error = value;
    }

    /// <inheritdoc/>
    public HttpStatusCode StatusCode
    {
        get => statusCode;
        init
        {
            statusCode = value;
            if (Error == null && !(((int)statusCode >= 200) && ((int)statusCode <= 299)))
            {
                HttpError = new()
                {
                    Message = "StatusCode: " + statusCode
                };
            }
        }
    }

    /// <summary>
    /// Appends <see cref="IHttpResult"/> results to the result.
    /// </summary>
    public IHttpResult? AppendHttpResult
    {
        init
        {
            if (value != null)
            {
                base.AppendResult = value;
                HttpError = value.HttpError;
                StatusCode = value.StatusCode;
            }
        }
    }

    /// <summary>
    /// Appends <see cref="IHttpResult"/> results to the result.
    /// </summary>
    public IHttpResult?[]? AppendHttpResults
    {
        init
        {
            if (value != null)
            {
                base.AppendResults = value;
                foreach (var result in value)
                {
                    AppendHttpResult = result;
                }
            }
        }
    }
}

/// <summary>
/// The base result for all HTTP requests.
/// </summary>
/// <inheritdoc/>
public class HttpResult<TResult> : Result<TResult>, IHttpResult
{
    private HttpStatusCode statusCode;

    /// <inheritdoc/>
    public HttpError? HttpError
    {
        get => Error as HttpError;
        init => Error = value;
    }

    /// <inheritdoc/>
    public HttpStatusCode StatusCode
    {
        get => statusCode;
        init
        {
            statusCode = value;
            if (Error == null && !(((int)statusCode >= 200) && ((int)statusCode <= 299)))
            {
                Error = new()
                {
                    Message = "StatusCode: " + statusCode
                };
            }
        }
    }

    /// <summary>
    /// Appends <see cref="IHttpResult"/> results to the result.
    /// </summary>
    public IHttpResult? AppendHttpResult
    {
        init
        {
            if (value != null)
            {
                base.AppendResult = value;
                HttpError = value.HttpError;
                StatusCode = value.StatusCode;
            }
        }
    }

    /// <summary>
    /// Appends <see cref="IHttpResult"/> results to the result.
    /// </summary>
    public IHttpResult?[]? AppendHttpResults
    {
        init
        {
            if (value != null)
            {
                base.AppendResults = value;
                foreach (var result in value)
                {
                    AppendHttpResult = result;
                }
            }
        }
    }
}
