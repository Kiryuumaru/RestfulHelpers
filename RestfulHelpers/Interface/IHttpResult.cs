using System.Diagnostics.CodeAnalysis;
using System;
using System.Collections.Generic;
using RestfulHelpers.Common;
using TransactionHelpers.Interface;
using TransactionHelpers;
using System.Net;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;

namespace RestfulHelpers.Interface;

/// <summary>
/// The interface for all HTTP responses.
/// </summary>
public interface IHttpResult : IResult, IActionResult
{
    internal HttpStatusCode InternalStatusCode { get; set; }

    internal Dictionary<string, string[]> InternalResponseHeaders { get; set; }

    /// <summary>
    /// Gets the http error of the response.
    /// </summary>
    [JsonIgnore]
    HttpError? HttpError { get; }

    /// <summary>
    /// Gets the status code of the response.
    /// </summary>
    HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Gets the HTTP response headers.
    /// </summary>
    [JsonIgnore]
    IReadOnlyDictionary<string, string[]> ResponseHeaders { get; }
}

/// <summary>
/// The interface for all HTTP responses.
/// </summary>
public interface IHttpResult<TValue> : IHttpResult, IResult<TValue>
{
}
