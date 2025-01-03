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
        httpResult.InternalStatusCode = statusCode;

        if ((int)statusCode < 200 || (int)statusCode > 299)
        {
            httpResult.WithError(new HttpError()
            {
                StatusCode = statusCode
            });
        }

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
        httpResult.InternalStatusCode = statusCode;

        if (problemDetails != null)
        {
            HttpError httpError = new();
            httpError.SetStatusCode(statusCode, problemDetails);
            httpResult.WithError(httpError);
        }
        else if ((int)statusCode < 200 || (int)statusCode > 299)
        {
            httpResult.WithError(new HttpError()
            {
                StatusCode = statusCode
            });
        }

        return httpResult;
    }

    /// <summary>
    /// Sets the HTTP result and status code for the given <paramref name="httpResult"/>.
    /// </summary>
    /// <typeparam name="T">Type of the HTTP result.</typeparam>
    /// <param name="httpResult">The HTTP result to modify.</param>
    /// <param name="appendResultValues">Append values if the results has the same value type.</param>
    /// <param name="httpResults">The HTTP results to set.</param>
    /// <returns>The modified HTTP result.</returns>
    public static T WithHttpResult<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(this T httpResult, bool appendResultValues, params IHttpResult[] httpResults)
        where T : IHttpResult
    {
        if (httpResults != null)
        {
            foreach (var r in httpResults)
            {
                if (r != null)
                {
                    httpResult.WithResult(appendResultValues, r);
                    if (typeof(T).GetProperty(nameof(IHttpResult.InternalStatusCode), BindingFlags.NonPublic | BindingFlags.Instance) is PropertyInfo resultStatCodeFieldInfo)
                    {
                        resultStatCodeFieldInfo.SetValue(httpResult, r.StatusCode);
                    }
                }
            }
        }
        return httpResult;
    }

    /// <summary>
    /// Sets the HTTP result and status code for the given <paramref name="httpResult"/>.
    /// </summary>
    /// <typeparam name="T">Type of the HTTP result.</typeparam>
    /// <param name="httpResult">The HTTP result to modify.</param>
    /// <param name="httpResults">The HTTP results to set.</param>
    /// <returns>The modified HTTP result.</returns>
    public static T WithHttpResult<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(this T httpResult, params IHttpResult[] httpResults)
        where T : IHttpResult
    {
        if (httpResults != null)
        {
            foreach (var r in httpResults)
            {
                if (r != null)
                {
                    httpResult.WithResult(r);
                    if (typeof(T).GetProperty(nameof(IHttpResult.InternalStatusCode), BindingFlags.NonPublic | BindingFlags.Instance) is PropertyInfo resultStatCodeFieldInfo)
                    {
                        resultStatCodeFieldInfo.SetValue(httpResult, r.StatusCode);
                    }
                }
            }
        }
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
