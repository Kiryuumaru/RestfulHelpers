using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace RestfulHelpers;

public class HttpResponse : Response
{
    public IReadOnlyList<HttpTransaction> HttpTransactions { get; }

    internal List<HttpTransaction> WritableHttpTransactions { get; }

    public HttpResponse()
    {
        WritableHttpTransactions = new();
        HttpTransactions = WritableHttpTransactions.AsReadOnly();
    }

    public void Append(params HttpTransaction[] httpTransactions)
    {
        WritableHttpTransactions.AddRange(httpTransactions);
    }

    public override void Append(params Response[] responses)
    {
        base.Append(responses);
        foreach (var response in responses)
        {
            if (response is HttpResponse httpResponse)
            {
                WritableHttpTransactions.AddRange(httpResponse.HttpTransactions);
            }
        }
    }

    public async Task<IEnumerable<StringHttpTransaction>> GetTransactionContentsAsString()
    {
        List<StringHttpTransaction> transactions = new();
        List<Task> tasks = new();

        for (int i = 0; i < HttpTransactions.Count; i++)
        {
            int index = i;
            transactions.Add(null!);
            tasks.Add(Task.Run(async () =>
            {
                string url = HttpTransactions[index].RequestUrl;
                string? requestContent = await HttpTransactions[index].GetRequestContentAsString();
                string? responseContent = await HttpTransactions[index].GetResponseContentAsString();
                HttpStatusCode statusCode = HttpTransactions[index].StatusCode;
                transactions[index] = new StringHttpTransaction(url, requestContent, responseContent, statusCode);
            }));
        }

        await Task.WhenAll(tasks);

        return transactions;
    }
}

public class HttpResponse<TResult> : Response<TResult>
{
    public IReadOnlyList<HttpTransaction> HttpTransactions { get; }

    private readonly List<HttpTransaction> WritableHttpTransactions;

    public HttpResponse()
    {
        WritableHttpTransactions = new();
        HttpTransactions = WritableHttpTransactions.AsReadOnly();
    }

    public void Append(params HttpTransaction[] httpTransactions)
    {
        WritableHttpTransactions.AddRange(httpTransactions);
    }

    public override void Append(params Response[] responses)
    {
        if (responses.LastOrDefault() is Response lastResponse)
        {
            Error = lastResponse.Error;
            if (lastResponse is HttpResponse<TResult> lastTypedResponse)
            {
                Result = lastTypedResponse.Result;
            }
        }
        foreach (var response in responses)
        {
            if (response is HttpResponse httpResponse)
            {
                WritableHttpTransactions.AddRange(httpResponse.HttpTransactions);
            }
        }
    }

    public async Task<IEnumerable<StringHttpTransaction>> GetTransactionContentsAsString()
    {
        List<StringHttpTransaction> transactions = new();
        List<Task> tasks = new();

        for (int i = 0; i < HttpTransactions.Count; i++)
        {
            int index = i;
            transactions.Add(null!);
            tasks.Add(Task.Run(async () =>
            {
                string url = HttpTransactions[index].RequestUrl;
                string? requestContent = await HttpTransactions[index].GetRequestContentAsString();
                string? responseContent = await HttpTransactions[index].GetResponseContentAsString();
                HttpStatusCode statusCode = HttpTransactions[index].StatusCode;
                transactions[index] = new StringHttpTransaction(url, requestContent, responseContent, statusCode);
            }));
        }

        await Task.WhenAll(tasks);

        return transactions;
    }
}
