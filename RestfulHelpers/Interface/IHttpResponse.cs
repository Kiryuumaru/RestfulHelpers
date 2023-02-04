using System.Diagnostics.CodeAnalysis;
using System;
using System.Collections.Generic;
using RestfulHelpers.Common;
using TransactionHelpers.Interface;

namespace RestfulHelpers.Interface;

/// <summary>
/// The interface for all HTTP responses.
/// </summary>
public interface IHttpResponse : IResponse
{
    /// <summary>
    /// Gets all http transactions made by the request.
    /// </summary>
    public IReadOnlyList<HttpTransaction> HttpTransactions { get; }
}
