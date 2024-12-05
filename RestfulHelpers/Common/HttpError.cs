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
    private HttpStatusCode statusCode;

    /// <summary>
    /// Gets the status code of the error.
    /// </summary>
    public HttpStatusCode StatusCode
    {
        get => statusCode;
        set
        {
            statusCode = value;
            ErrorCode = statusCode.ToString().ToSnakeCase().ToUpper();
            if (string.IsNullOrEmpty(Message))
            {
                Message = "StatusCode: " + statusCode;
            }
        }
    }
}
