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
    }

    internal void SetStatusCode(HttpStatusCode statusCode, ProblemDetails? problemDetails)
    {
        problemDetails ??= new ProblemDetails();
        problemDetails.Status = (int)statusCode;
        Code = statusCode.ToString().ToSnakeCase().ToUpper();
        Detail = problemDetails;
        Message = "StatusCode: " + statusCode.ToString();
    }

    internal void SetStatusCode(HttpStatusCode statusCode, string? errorMessage = null, string? errorCode = null, string? errorTitle = null, string? errorDetail = null, string? errorInstance = null, string? errorType = null, IDictionary<string, object?>? errorExtensions = null)
    {
        var problemDetails = new ProblemDetails()
        {
            Title = errorTitle,
            Detail = errorDetail,
            Instance = errorInstance,
            Type = errorType,
            Status = (int)statusCode
        };
        if (errorExtensions != null)
        {
            problemDetails.Extensions.Clear();
            foreach (var extension in errorExtensions)
            {
                problemDetails.Extensions.Add(extension.Key, extension.Value);
            }
        }
        Code = string.IsNullOrEmpty(errorCode) ? statusCode.ToString().ToSnakeCase().ToUpper() : errorCode;
        Message = string.IsNullOrEmpty(errorMessage) ? "StatusCode: " + statusCode.ToString() : errorMessage;
        Detail = problemDetails;
    }

    /// <inheritdoc/>
    public override object Clone()
    {
        HttpError httpError = new();
        HandleClone(httpError);
        return httpError;
    }
}
