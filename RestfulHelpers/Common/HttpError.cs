using Microsoft.AspNetCore.Mvc;
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
    [JsonIgnore]
    public HttpStatusCode StatusCode
    {
        get => (HttpStatusCode)((Detail as Microsoft.AspNetCore.Mvc.ProblemDetails)?.Status ?? default);
        set
        {
            Code = value.ToString().ToSnakeCase().ToUpper();
            Detail = new ProblemDetails() { Status = (int)value };
            if (string.IsNullOrEmpty(Message))
            {
                Message = "StatusCode: " + value;
            }
        }
    }

    internal void SetStatusCode(HttpStatusCode statusCode, Microsoft.AspNetCore.Mvc.ProblemDetails problemDetails)
    {
        StatusCode = statusCode;
        Detail = problemDetails;
    }
}
