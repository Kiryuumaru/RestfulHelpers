using RestfulHelpers;
using RestfulHelpers.Common;
using RestfulHelpers.Common.Internals;
using RestfulHelpers.Interface;
using System;
using System.Collections.Generic;
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
    /// <param name="statusCodes">The HTTP status codes to set.</param>
    /// <returns>The modified HTTP result.</returns>
    public static T WithStatusCode<T>(this T httpResult, params HttpStatusCode[] statusCodes)
        where T : IHttpResult
    {
        if (statusCodes != null)
        {
            foreach (var statusCode in statusCodes)
            {
                if (httpResult.GetType().GetField(nameof(HttpResult.InternalStatusCode), BindingFlags.NonPublic | BindingFlags.Instance) is FieldInfo resultStatCodeFieldInfo)
                {
                    resultStatCodeFieldInfo.SetValue(httpResult, statusCode);
                }

                if ((int)statusCode < 200 || (int)statusCode > 299)
                {
                    httpResult.WithError(new HttpError()
                    {
                        Message = "StatusCode: " + statusCode,
                        StatusCode = statusCode
                    });
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
    public static T WithHttpResult<T>(this T httpResult, params IHttpResult[] httpResults)
        where T : IHttpResult
    {
        if (httpResults != null)
        {
            foreach (var r in httpResults)
            {
                if (r != null)
                {
                    httpResult.WithResult(r);
                    if (httpResult.GetType().GetField(nameof(HttpResult.InternalStatusCode), BindingFlags.NonPublic | BindingFlags.Instance) is FieldInfo resultStatCodeFieldInfo)
                    {
                        resultStatCodeFieldInfo.SetValue(httpResult, r.StatusCode);
                    }
                }
            }
        }
        return httpResult;
    }
}
