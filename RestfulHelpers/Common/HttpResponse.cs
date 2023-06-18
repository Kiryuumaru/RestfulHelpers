using RestfulHelpers;
using RestfulHelpers.Common;
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
    /// <inheritdoc/>
    public HttpError? HttpError
    {
        get => Error as HttpError;
        set => Error = value;
    }

    /// <inheritdoc/>
    public HttpStatusCode StatusCode { get; protected set; }

    /// <summary>
    /// Creates new instance of <see cref="HttpResponse"/>
    /// </summary>
    public HttpResponse()
    {

    }

    /// <summary>
    /// Creates new instance of <see cref="HttpResponse"/>
    /// </summary>
    /// <param name="error">The <see cref="Error"/> to initially append.</param>
    /// <param name="statusCode">The <see cref="HttpStatusCode"/> to initially append.</param>
    [JsonConstructor]
    public HttpResponse(Error? error, HttpStatusCode statusCode)
    {
        Append(statusCode);
        Append(error);
    }

    /// <summary>
    /// Appends <see cref="IHttpResponse"/> responses to the response.
    /// </summary>
    /// <param name="responses">The <see cref="IHttpResponse"/> to append.</param>
    public virtual void Append(params IHttpResponse[] responses)
    {
        if (responses.LastOrDefault() is IHttpResponse lastResponse)
        {
            HttpError = lastResponse.HttpError;
            Error = lastResponse.Error;
            StatusCode = lastResponse.StatusCode;
        }
    }

    /// <summary>
    /// Appends <see cref="HttpStatusCode"/> to the response.
    /// </summary>
    /// <param name="httpStatusCode">The <see cref="HttpStatusCode"/> to append.</param>
    public virtual void Append(HttpStatusCode httpStatusCode)
    {
        StatusCode = httpStatusCode;
        if (!(((int)httpStatusCode >= 200) && ((int)httpStatusCode <= 299)))
        {
            Append(new HttpError(null, null, httpStatusCode));
        }
    }

    /// <summary>
    /// Appends <see cref="Exception"/> and <see cref="HttpStatusCode"/> to the response.
    /// </summary>
    /// <param name="exception">The <see cref="System.Exception"/> to append.</param>
    /// <param name="httpStatusCode">The <see cref="HttpStatusCode"/> to append.</param>
    public virtual void Append(Exception? exception, HttpStatusCode httpStatusCode)
    {
        Append(exception);
        StatusCode = httpStatusCode;
        if (!(((int)httpStatusCode >= 200) && ((int)httpStatusCode <= 299)))
        {
            Append(new HttpError(exception, exception?.Message, httpStatusCode));
        }
    }
}

/// <summary>
/// The base response for all HTTP requests.
/// </summary>
/// <inheritdoc/>
public class HttpResponse<TResult> : Response<TResult>, IHttpResponse
{
    /// <inheritdoc/>
    public HttpError? HttpError
    {
        get => Error as HttpError;
        set => Error = value;
    }

    /// <inheritdoc/>
    public HttpStatusCode StatusCode { get; protected set; }

    /// <summary>
    /// Creates new instance of <see cref="HttpResponse{TResult}"/>
    /// </summary>
    public HttpResponse()
    {

    }

    /// <summary>
    /// Creates new instance of <see cref="HttpResponse{TResult}"/>
    /// </summary>
    /// <param name="result">The <typeparamref name="TResult"/> to initially append.</param>
    /// <param name="error">The <see cref="Error"/> to initially append.</param>
    /// <param name="statusCode">The <see cref="HttpStatusCode"/> to initially append.</param>
    [JsonConstructor]
    public HttpResponse(TResult? result, Error? error, HttpStatusCode statusCode)
    {
        Append(result);
        Append(statusCode);
        Append(error);
    }

    /// <summary>
    /// Appends <see cref="IHttpResponse"/> responses to the response.
    /// </summary>
    /// <param name="httpStatusCode">The <see cref="HttpStatusCode"/> to append.</param>
    public virtual void Append(HttpStatusCode httpStatusCode)
    {
        StatusCode = httpStatusCode;
        if (!(((int)httpStatusCode >= 200) && ((int)httpStatusCode <= 299)))
        {
            Append(new HttpError(null, null, httpStatusCode));
        }
    }

    /// <summary>
    /// Appends <see cref="Exception"/> and <see cref="HttpStatusCode"/> to the response.
    /// </summary>
    /// <param name="exception">The <see cref="System.Exception"/> to append.</param>
    /// <param name="httpStatusCode">The <see cref="HttpStatusCode"/> to append.</param>
    public virtual void Append(Exception? exception, HttpStatusCode httpStatusCode)
    {
        StatusCode = httpStatusCode;
        Append(exception);
        if (!(((int)httpStatusCode >= 200) && ((int)httpStatusCode <= 299)))
        {
            Append(new HttpError(exception, exception?.Message, httpStatusCode));
        }
    }

    /// <summary>
    /// Appends <see cref="IHttpResponse"/> responses to the response.
    /// </summary>
    /// <param name="responses">The <see cref="IHttpResponse"/> to append.</param>
    public virtual void Append(params IHttpResponse[] responses)
    {
        if (responses.LastOrDefault() is IHttpResponse lastResponse)
        {
            HttpError = lastResponse.HttpError;
            Error = lastResponse.Error;
            if (lastResponse is HttpResponse<TResult> lastTypedResponse)
            {
                Result = lastTypedResponse.Result;
                StatusCode = lastTypedResponse.StatusCode;
            }
        }
    }
}
