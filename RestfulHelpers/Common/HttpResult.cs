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
    internal HttpStatusCode InternalStatusCode = default;

    /// <inheritdoc/>
    public HttpError? HttpError => Error as HttpError;

    /// <inheritdoc/>
    public HttpStatusCode StatusCode => InternalStatusCode;
}

/// <summary>
/// The base result for all HTTP requests.
/// </summary>
/// <inheritdoc/>
public class HttpResult<TValue> : Result<TValue>, IHttpResult<TValue>
{
    internal HttpStatusCode InternalStatusCode = default;

    /// <inheritdoc/>
    public HttpError? HttpError => Error as HttpError;

    /// <inheritdoc/>
    public HttpStatusCode StatusCode => InternalStatusCode;
}
