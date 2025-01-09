using Microsoft.AspNetCore.Mvc;
using RestfulHelpers;
using RestfulHelpers.Common;
using RestfulHelpers.Common.Internals;
using RestfulHelpers.Interface;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
/// The fluent methods for <see cref="HttpResult"/>.
/// </summary>
public static class HttpResultExtension
{
    /// <summary>
    /// Sets the HTTP status code for the given <paramref name="httpResult"/>.
    /// </summary>
    /// <typeparam name="T">Type of the HTTP result.</typeparam>
    /// <param name="httpResult">The HTTP result to modify.</param>
    /// <param name="statusCode">The HTTP status code to set.</param>
    /// <returns>The modified HTTP result.</returns>
    public static T WithStatusCode<T>(this T httpResult, HttpStatusCode statusCode)
        where T : IHttpResult
    {
        httpResult.Append(new HttpResultAppend() { StatusCode = statusCode, ShouldAppendStatusCodeOrError = true });
        return httpResult;
    }

    /// <summary>
    /// Sets the HTTP status code for the given <paramref name="httpResult"/>.
    /// </summary>
    /// <typeparam name="T">Type of the HTTP result.</typeparam>
    /// <typeparam name="TProblemDetails">Type of the problem details.</typeparam>
    /// <param name="httpResult">The HTTP result to modify.</param>
    /// <param name="statusCode">The HTTP status code to set.</param>
    /// <param name="problemDetails">Optional problem details to include in the error.</param>
    /// <returns>The modified HTTP result.</returns>
    public static T WithStatusCode<T, TProblemDetails>(this T httpResult, HttpStatusCode statusCode, TProblemDetails? problemDetails = null)
        where T : IHttpResult
        where TProblemDetails : ProblemDetails
    {
        if (problemDetails != null)
        {
            HttpError httpError = new();
            httpError.SetStatusCode(statusCode, problemDetails);
            httpResult.Append(new HttpResultAppend() { Errors = [httpError], StatusCode = statusCode, ShouldAppendErrors = true, ShouldAppendStatusCode = true });
        }
        else
        {
            httpResult.Append(new HttpResultAppend() { StatusCode = statusCode, ShouldAppendStatusCodeOrError = true });
        }

        return httpResult;
    }

    /// <summary>
    /// Sets the HTTP status code for the given <paramref name="httpResult"/>.
    /// </summary>
    /// <typeparam name="T">Type of the HTTP result.</typeparam>
    /// <param name="httpResult">The HTTP result to modify.</param>
    /// <param name="statusCode">The HTTP status code to set.</param>
    /// <param name="errorMessage">Optional error message to include in the error.</param>
    /// <param name="errorCode">Optional error code to include in the error.</param>
    /// <param name="errorTitle">Optional error title to include in the error.</param>
    /// <param name="errorDetail">Optional error detail to include in the error.</param>
    /// <param name="errorInstance">Optional error instance to include in the error.</param>
    /// <param name="errorType">Optional error type to include in the error.</param>
    /// <param name="errorExtensions">Optional error extensions to include in the error.</param>
    /// <returns>The modified HTTP result.</returns>
    public static T WithStatusCode<T>(this T httpResult, HttpStatusCode statusCode, string? errorMessage = null, string? errorCode = null, string? errorTitle = null, string? errorDetail = null, string? errorInstance = null, string? errorType = null, IDictionary<string, object?>? errorExtensions = null)
        where T : IHttpResult
    {
        if (!string.IsNullOrEmpty(errorMessage) || !string.IsNullOrEmpty(errorCode) || !string.IsNullOrEmpty(errorTitle) || !string.IsNullOrEmpty(errorDetail) || !string.IsNullOrEmpty(errorInstance) || !string.IsNullOrEmpty(errorType) || errorExtensions != null)
        {
            HttpError httpError = new();
            var problemDetails = new ProblemDetails()
            {
                Status = (int)statusCode,
                Title = errorTitle,
                Detail = errorDetail,
                Instance = errorInstance,
                Type = errorType
            };
            if (errorExtensions != null)
            {
                problemDetails.Extensions.Clear();
                foreach (var extension in errorExtensions)
                {
                    problemDetails.Extensions.Add(extension.Key, extension.Value);
                }
            }
            httpError.Code = errorCode;
            httpError.Message = errorMessage;
            httpError.Detail = problemDetails;
            httpResult.Append(new HttpResultAppend() { Errors = [httpError], StatusCode = statusCode, ShouldAppendErrors = true, ShouldAppendStatusCode = true });
        }
        else
        {
            httpResult.Append(new HttpResultAppend() { StatusCode = statusCode, ShouldAppendStatusCodeOrError = true });
        }

        return httpResult;
    }

    /// <summary>
    /// Adds a http response header to the given <paramref name="httpResult"/>.
    /// </summary>
    /// <typeparam name="T">Type of the HTTP result.</typeparam>
    /// <param name="httpResult">The HTTP result to modify.</param>
    /// <param name="headerName">The name of the header to add.</param>
    /// <param name="headerValues">The values of the header to add.</param>
    /// <returns>The modified HTTP result.</returns>
    public static T WithHttpResponseHeader<T>(this T httpResult, string headerName, params string[] headerValues)
        where T : IHttpResult
    {
        httpResult.Append(new HttpResultAppend() { ResponseHeaders = new Dictionary<string, string[]>() { [headerName] = headerValues }, ShouldReplaceHeaders = true });
        return httpResult;
    }

    /// <summary>
    /// Appends a http response header to the given <paramref name="httpResult"/>.
    /// </summary>
    /// <typeparam name="T">Type of the HTTP result.</typeparam>
    /// <param name="httpResult">The HTTP result to modify.</param>
    /// <param name="headerName">The name of the header to add.</param>
    /// <param name="headerValues">The values of the header to add.</param>
    /// <returns>The modified HTTP result.</returns>
    public static T WithHttpResponseHeaderAppend<T>(this T httpResult, string headerName, params string[] headerValues)
        where T : IHttpResult
    {
        httpResult.Append(new HttpResultAppend() { ResponseHeaders = new Dictionary<string, string[]>() { [headerName] = headerValues }, ShouldAppendHeaders = true });
        return httpResult;
    }

    /// <summary>
    /// Converts <paramref name="httpResult"/> to its corresponding <see cref="ActionResult{T}"/>.
    /// </summary>
    /// <typeparam name="T">Type of the HTTP result.</typeparam>
    /// <param name="httpResult">The HTTP result to convert.</param>
    /// <returns>The modified HTTP result.</returns>
    public static ActionResult<T> ToActionResult<T>(this T httpResult)
        where T : IHttpResult
    {
        return new ObjectResult(httpResult)
        {
            DeclaredType = httpResult.GetType(),
            StatusCode = (int)httpResult.StatusCode,
        };
    }
}
