using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
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
        get
        {
            if (Detail is ProblemDetails problemDetails)
            {
                return (HttpStatusCode)(problemDetails.Status ?? default);
            }
            else if (Detail is JsonElement problemDetailsJson &&
                problemDetailsJson.ValueKind == JsonValueKind.Object &&
                problemDetailsJson
                    .EnumerateObject()
                    .FirstOrDefault(p => string.Compare(p.Name, "status", StringComparison.InvariantCultureIgnoreCase) == 0).Value is JsonElement statusProp &&
                statusProp.ValueKind == JsonValueKind.Number &&
                statusProp.TryGetInt32(out int statusInt))
            {
                return (HttpStatusCode)statusInt;
            }

            return 0;
        }
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

    internal void SetStatusCode(HttpStatusCode statusCode, ProblemDetails problemDetails)
    {
        StatusCode = statusCode;
        Detail = problemDetails;
    }
}
