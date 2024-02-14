using System.Diagnostics.CodeAnalysis;
using System;
using System.Collections.Generic;
using RestfulHelpers.Common;
using TransactionHelpers.Interface;
using TransactionHelpers;
using System.Net;

namespace RestfulHelpers.Interface;

/// <summary>
/// The interface for all HTTP responses.
/// </summary>
public interface IHttpResult : IResult
{
    /// <summary>
    /// Gets the http error of the response.
    /// </summary>
    HttpError? HttpError { get; }

    /// <summary>
    /// Gets the status code of the response.
    /// </summary>
    HttpStatusCode StatusCode { get; }
}
