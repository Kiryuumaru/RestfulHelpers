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
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TransactionHelpers;

namespace RestfulHelpers.Common;

/// <summary>
/// The base response for all HTTP requests.
/// </summary>
public class HttpResponse : Response, IHttpResponse
{
    private HttpStatusCode statusCode;

    /// <inheritdoc/>
    public HttpError? HttpError
    {
        get => Error as HttpError;
        init => Error = value;
    }

    /// <inheritdoc/>
    public HttpStatusCode StatusCode
    {
        get => statusCode;
        init
        {
            statusCode = value;
            if (Error == null && !(((int)statusCode >= 200) && ((int)statusCode <= 299)))
            {
                Error = new()
                {
                    Message = "StatusCode: " + statusCode
                };
            }
        }
    }

    /// <summary>
    /// Appends <see cref="IHttpResponse"/> responses to the response.
    /// </summary>
    /// <param name="responses">The <see cref="IHttpResponse"/> to append.</param>
    /// <returns>The resulting <see cref="HttpResponse"/> after the append.</returns>
    public virtual HttpResponse Append(params IHttpResponse[] responses)
    {
        if (responses.LastOrDefault() is IHttpResponse lastResponse)
        {
            return new()
            {
                Error = lastResponse.Error,
                HttpError = lastResponse.HttpError,
                StatusCode = lastResponse.StatusCode,
            };
        }
        return this;
    }

    /// <summary>
    /// Appends <see cref="HttpStatusCode"/> to the response.
    /// </summary>
    /// <param name="httpStatusCode">The <see cref="HttpStatusCode"/> to append.</param>
    /// <returns>The resulting <see cref="HttpResponse"/> after the append.</returns>
    public virtual HttpResponse Append(HttpStatusCode httpStatusCode)
    {
        return new()
        {
            Error = Error,
            HttpError = HttpError,
            StatusCode = httpStatusCode
        };
    }

    /// <summary>
    /// Appends <see cref="Exception"/> and <see cref="HttpStatusCode"/> to the response.
    /// </summary>
    /// <param name="exception">The <see cref="System.Exception"/> to append.</param>
    /// <param name="httpStatusCode">The <see cref="HttpStatusCode"/> to append.</param>
    /// <returns>The resulting <see cref="HttpResponse"/> after the append.</returns>
    public virtual HttpResponse Append(Exception? exception, HttpStatusCode httpStatusCode)
    {
        if (exception == null)
        {
            return new()
            {
                Error = Error,
                HttpError = HttpError,
                StatusCode = httpStatusCode
            };
        }
        return new()
        {
            HttpError = HttpError,
            Error = new() { Exception = exception },
            StatusCode = httpStatusCode
        };
    }
}

/// <summary>
/// The base response for all HTTP requests.
/// </summary>
/// <inheritdoc/>
public class HttpResponse<TResult> : Response<TResult>, IHttpResponse
{
    private HttpStatusCode statusCode;

    /// <inheritdoc/>
    public HttpError? HttpError
    {
        get => Error as HttpError;
        init => Error = value;
    }

    /// <inheritdoc/>
    public HttpStatusCode StatusCode
    {
        get => statusCode;
        init
        {
            statusCode = value;
            if (Error == null && !(((int)statusCode >= 200) && ((int)statusCode <= 299)))
            {
                Error = new()
                {
                    Message = "StatusCode: " + statusCode
                };
            }
        }
    }

    /// <summary>
    /// Appends <typeparamref name="TResult"/> to the response.
    /// </summary>
    /// <param name="result">The <typeparamref name="TResult"/> to append.</param>
    /// <returns>The resulting <see cref="HttpResponse{TResult}"/> after the append.</returns>
    public new virtual HttpResponse<TResult> Append(TResult result)
    {
        return new()
        {
            Error = Error,
            HttpError = HttpError,
            StatusCode = StatusCode,
            Result = result,
        };
    }

    /// <summary>
    /// Appends <see cref="IHttpResponse"/> responses to the response.
    /// </summary>
    /// <param name="httpStatusCode">The <see cref="HttpStatusCode"/> to append.</param>
    /// <returns>The resulting <see cref="HttpResponse{TResult}"/> after the append.</returns>
    public virtual HttpResponse<TResult> Append(HttpStatusCode httpStatusCode)
    {
        return new()
        {
            Result = Result,
            Error = Error,
            HttpError = HttpError,
            StatusCode = httpStatusCode
        };
    }

    /// <summary>
    /// Appends <see cref="Exception"/> and <see cref="HttpStatusCode"/> to the response.
    /// </summary>
    /// <param name="exception">The <see cref="System.Exception"/> to append.</param>
    /// <param name="httpStatusCode">The <see cref="HttpStatusCode"/> to append.</param>
    /// <returns>The resulting <see cref="HttpResponse{TResult}"/> after the append.</returns>
    public virtual HttpResponse<TResult> Append(Exception? exception, HttpStatusCode httpStatusCode)
    {
        if (exception == null)
        {
            return new()
            {
                Result = Result,
                Error = Error,
                HttpError = HttpError,
                StatusCode = httpStatusCode
            };
        }
        return new()
        {
            Result = Result,
            HttpError = HttpError,
            Error = new() { Exception = exception },
            StatusCode = httpStatusCode
        };
    }

    /// <summary>
    /// Appends <see cref="IHttpResponse"/> responses to the response.
    /// </summary>
    /// <param name="responses">The <see cref="IHttpResponse"/> to append.</param>
    /// <returns>The resulting <see cref="HttpResponse{TResult}"/> after the append.</returns>
    public virtual HttpResponse<TResult> Append(params IHttpResponse[] responses)
    {
        if (responses.LastOrDefault() is IHttpResponse lastResponse)
        {
            if (lastResponse is HttpResponse<TResult> lastTypedResponse)
            {
                return new()
                {
                    Result = lastTypedResponse.Result,
                    Error = lastResponse.Error,
                    HttpError = lastResponse.HttpError,
                    StatusCode = lastResponse.StatusCode
                };
            }
            return new()
            {
                Result = Result,
                Error = lastResponse.Error,
                HttpError = lastResponse.HttpError,
                StatusCode = lastResponse.StatusCode
            };
        }
        return this;
    }
}
