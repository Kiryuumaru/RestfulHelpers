using RestfulHelpers;
using RestfulHelpers.Common;
using RestfulHelpers.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace RestfulHelpers.Common;

internal static class HttpResponseCommon
{
    internal static async Task<IEnumerable<StringHttpTransaction>> GetTransactionContentsAsString(IHttpResponse response)
    {
        List<StringHttpTransaction> transactions = new();
        List<Task> tasks = new();

        for (int i = 0; i < response.HttpTransactions.Count; i++)
        {
            int index = i;
            transactions.Add(null!);
            tasks.Add(Task.Run(async () =>
            {
                string? url = response.HttpTransactions[index].RequestUrl;
                string? requestContent = await response.HttpTransactions[index].GetRequestContentAsString();
                string? responseContent = await response.HttpTransactions[index].GetResponseContentAsString();
                HttpStatusCode? statusCode = response.HttpTransactions[index].StatusCode;
                transactions[index] = new StringHttpTransaction(url, requestContent, responseContent, statusCode);
            }));
        }

        await Task.WhenAll(tasks);

        return transactions;
    }
}

/// <summary>
/// The base response for all HTTP requests.
/// </summary>
public class HttpResponse : Response, IHttpResponse
{
    /// <inheritdoc/>
    public IReadOnlyList<HttpTransaction> HttpTransactions { get; }

    private readonly List<HttpTransaction> httpTransactions;

    /// <summary>
    /// Creates new instance of <see cref="HttpResponse"/>
    /// </summary>
    public HttpResponse()
        : this(default(Exception))
    {

    }

    /// <summary>
    /// Creates new instance of <see cref="HttpResponse"/>
    /// </summary>
    /// <param name="response">The <see cref="IHttpResponse"/> to initially append.</param>
    public HttpResponse(IHttpResponse response)
        : this(response.Error)
    {
        httpTransactions.AddRange(response.HttpTransactions);
    }

    /// <summary>
    /// Creates new instance of <see cref="HttpResponse"/>
    /// </summary>
    /// <param name="response">The <see cref="IHttpResponse"/> to initially append.</param>
    /// <param name="error">The <see cref="Exception"/> to initially append.</param>
    public HttpResponse(IHttpResponse response, Exception? error)
        : this(error)
    {
        httpTransactions.AddRange(response.HttpTransactions);
    }

    /// <summary>
    /// Creates new instance of <see cref="HttpResponse"/>
    /// </summary>
    /// <param name="request">The <see cref="HttpRequestMessage"/> of <see cref="HttpTransaction.RequestMessage"/> to initially append.</param>
    /// <param name="response">The <see cref="HttpResponseMessage"/> of <see cref="HttpTransaction.ResponseMessage"/> to initially append.</param>
    /// <param name="httpStatusCode">The <see cref="HttpStatusCode"/> of <see cref="HttpTransaction.StatusCode"/> to initially append.</param>
    /// <param name="error">The <see cref="Exception"/> to initially append.</param>
    public HttpResponse(HttpRequestMessage request, HttpResponseMessage response, HttpStatusCode httpStatusCode, Exception? error)
        : this(error)
    {
        httpTransactions.Add(new(request, response, httpStatusCode));
    }

    /// <summary>
    /// Creates new instance of <see cref="HttpResponse"/>
    /// </summary>
    /// <param name="error">The <see cref="Exception"/> to initially append.</param>
    public HttpResponse(Exception? error)
    {
        httpTransactions = new();
        HttpTransactions = httpTransactions.AsReadOnly();

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
            Error = lastResponse.Error;
        }
        foreach (var response in responses)
        {
            httpTransactions.AddRange(response.HttpTransactions);
        }
    }

    /// <summary>
    /// Appends <see cref="HttpTransaction"/> transactions to the response.
    /// </summary>
    /// <param name="transactions">The <see cref="HttpTransaction"/> to append.</param>
    public virtual void Append(params HttpTransaction[] transactions)
    {
        httpTransactions.AddRange(transactions);
    }

    /// <summary>
    /// Gets the string representation of the HTTP transactions.
    /// </summary>
    /// <returns>
    /// The <see cref="Task"/> that represents the string transaction contents.
    /// </returns>
    public Task<IEnumerable<StringHttpTransaction>> GetTransactionContentsAsString() => HttpResponseCommon.GetTransactionContentsAsString(this);
}

/// <summary>
/// The base response for all HTTP requests.
/// </summary>
/// <inheritdoc/>
public class HttpResponse<TResult> : Response<TResult>, IHttpResponse
{
    /// <inheritdoc/>
    public IReadOnlyList<HttpTransaction> HttpTransactions { get; }

    private readonly List<HttpTransaction> httpTransactions;

    /// <summary>
    /// Creates new instance of <see cref="HttpResponse{TResult}"/>
    /// </summary>
    public HttpResponse()
        : this(default(TResult), default(Exception))
    {

    }

    /// <summary>
    /// Creates new instance of <see cref="HttpResponse{TResult}"/>
    /// </summary>
    /// <param name="result">The <typeparamref name="TResult"/> to initially append.</param>
    public HttpResponse(TResult result)
        : this(result, default(Exception))
    {

    }

    /// <summary>
    /// Creates new instance of <see cref="HttpResponse{TResult}"/>
    /// </summary>
    /// <param name="response">The <see cref="IHttpResponse"/> to initially append.</param>
    public HttpResponse(IHttpResponse response)
        : this(default(TResult), default(Exception))
    {
        httpTransactions.AddRange(response.HttpTransactions);
    }

    /// <summary>
    /// Creates new instance of <see cref="HttpResponse{TResult}"/>
    /// </summary>
    /// <param name="result">The <typeparamref name="TResult"/> to initially append.</param>
    /// <param name="response">The <see cref="IHttpResponse"/> to initially append.</param>
    public HttpResponse(TResult? result, IHttpResponse response)
        : this(result, response.Error)
    {
        httpTransactions.AddRange(response.HttpTransactions);
    }

    /// <summary>
    /// Creates new instance of <see cref="HttpResponse{TResult}"/>
    /// </summary>
    /// <param name="response">The <see cref="IHttpResponse"/> to initially append.</param>
    /// <param name="error">The <see cref="Exception"/> to initially append.</param>
    public HttpResponse(IHttpResponse response, Exception? error)
        : this(default(TResult), error)
    {
        httpTransactions.AddRange(response.HttpTransactions);
    }

    /// <summary>
    /// Creates new instance of <see cref="HttpResponse{TResult}"/>
    /// </summary>
    /// <param name="result">The <typeparamref name="TResult"/> to initially append.</param>
    /// <param name="response">The <see cref="IHttpResponse"/> to initially append.</param>
    /// <param name="error">The <see cref="Exception"/> to initially append.</param>
    public HttpResponse(TResult? result, IHttpResponse response, Exception? error)
        : this(result, error)
    {
        httpTransactions.AddRange(response.HttpTransactions);
    }

    /// <summary>
    /// Creates new instance of <see cref="HttpResponse{TResult}"/>
    /// </summary>
    /// <param name="result">The <typeparamref name="TResult"/> to initially append.</param>
    /// <param name="request">The <see cref="HttpRequestMessage"/> of <see cref="HttpTransaction.RequestMessage"/> to initially append.</param>
    /// <param name="response">The <see cref="HttpResponseMessage"/> of <see cref="HttpTransaction.ResponseMessage"/> to initially append.</param>
    /// <param name="httpStatusCode">The <see cref="HttpStatusCode"/> of <see cref="HttpTransaction.StatusCode"/> to initially append.</param>
    /// <param name="error">The <see cref="Exception"/> to initially append.</param>
    public HttpResponse(TResult? result, HttpRequestMessage request, HttpResponseMessage response, HttpStatusCode httpStatusCode, Exception? error)
        : this(result, error)
    {
        httpTransactions.Add(new(request, response, httpStatusCode));
    }

    /// <summary>
    /// Creates new instance of <see cref="HttpResponse{TResult}"/>
    /// </summary>
    /// <param name="result"></param>
    /// <param name="error"></param>
    public HttpResponse(TResult? result, Exception? error)
    {
        httpTransactions = new();
        HttpTransactions = httpTransactions.AsReadOnly();

        Append(result);
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
            Error = lastResponse.Error;
            if (lastResponse is HttpResponse<TResult> lastTypedResponse)
            {
                Result = lastTypedResponse.Result;
            }
        }
        foreach (var response in responses)
        {
            httpTransactions.AddRange(response.HttpTransactions);
        }
    }

    /// <summary>
    /// Appends <see cref="HttpTransaction"/> transactions to the response.
    /// </summary>
    /// <param name="transactions">The <see cref="HttpTransaction"/> to append.</param>
    public virtual void Append(params HttpTransaction[] transactions)
    {
        httpTransactions.AddRange(transactions);
    }

    /// <summary>
    /// Gets the string representation of the HTTP transactions.
    /// </summary>
    /// <returns>
    /// The <see cref="Task"/> that represents the string transaction contents.
    /// </returns>
    public Task<IEnumerable<StringHttpTransaction>> GetTransactionContentsAsString() => HttpResponseCommon.GetTransactionContentsAsString(this);
}
