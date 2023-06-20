using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json.Serialization;
using TransactionHelpers;

namespace RestfulHelpers.Common;

/// <summary>
/// The error representation for http errors.
/// </summary>
public class HttpError : Error
{
    /// <summary>
    /// Gets the status code of the error.
    /// </summary>
    public HttpStatusCode StatusCode { get; init; }

    /// <summary>
    /// Creates new instance of <see cref="HttpError"/>.
    /// </summary>
    public HttpError()
    {

    }

    /// <summary>
    /// Creates new instance of <see cref="HttpError"/>.
    /// </summary>
    /// <param name="exception">The <see cref="System.Exception"/> of the error.</param>
    /// <param name="message">The message of the error.</param>
    public HttpError(Exception? exception, string? message)
        : base(exception, message)
    {
    }

    /// <summary>
    /// Creates new instance of <see cref="HttpError"/>.
    /// </summary>
    /// <param name="exception">The <see cref="System.Exception"/> of the error.</param>
    /// <param name="message">The message of the error.</param>
    /// <param name="statusCode">The <see cref="HttpStatusCode"/> of the error.</param>
    [JsonConstructor]
    public HttpError(Exception? exception, string? message, HttpStatusCode statusCode)
        : base(exception, message)
    {
        StatusCode = statusCode;
        if (string.IsNullOrEmpty(Message))
        {
            Message = "StatusCode: " + statusCode;
        }
    }
}
