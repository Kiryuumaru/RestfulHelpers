using System.IO;
using System.Net;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace RestfulHelpers;

internal static class HttpHelpers
{
    internal static async Task<HttpResponse> Execute(HttpClient httpClient, HttpRequestMessage httpRequestMessage, HttpCompletionOption httpCompletionOption, CancellationToken cancellationToken)
    {
        HttpResponseMessage? httpResponseMessage = null;
        HttpStatusCode statusCode = HttpStatusCode.OK;
        HttpResponse response = new();

        if (httpRequestMessage.Content != null)
        {
            await httpRequestMessage.Content.LoadIntoBufferAsync();
        }

        try
        {
            httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, httpCompletionOption, cancellationToken);

            statusCode = httpResponseMessage.StatusCode;

            httpResponseMessage.EnsureSuccessStatusCode();

            response.Append(new HttpTransaction(httpRequestMessage, httpResponseMessage, statusCode));
        }
        catch (Exception ex)
        {
            response.Append(new HttpTransaction(httpRequestMessage, httpResponseMessage, statusCode));
            response.Append(ex);
        }

        return response;
    }

    internal static async Task<HttpResponse<T>> Execute<T>(HttpClient httpClient, HttpRequestMessage httpRequestMessage, HttpCompletionOption httpCompletionOption, JsonSerializerOptions jsonSerializerOptions, CancellationToken cancellationToken)
    {
        HttpResponseMessage? httpResponseMessage = null;
        HttpStatusCode statusCode = HttpStatusCode.OK;
        HttpResponse<T> response = new();

        if (httpRequestMessage.Content != null)
        {
            await httpRequestMessage.Content.LoadIntoBufferAsync();
        }

        try
        {
            httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, httpCompletionOption, cancellationToken);

            statusCode = httpResponseMessage.StatusCode;

            httpResponseMessage.EnsureSuccessStatusCode();

#if NETSTANDARD
            var httpResponseMessageData = await httpResponseMessage.Content.ReadAsStringAsync();
#else
            var httpResponseMessageData = await httpResponseMessage.Content.ReadAsStringAsync(cancellationToken);
#endif

            response.Append(new HttpTransaction(httpRequestMessage, httpResponseMessage, statusCode));
            response.Append(JsonSerializer.Deserialize<T>(httpResponseMessageData, jsonSerializerOptions));
        }
        catch (Exception ex)
        {
            response.Append(new HttpTransaction(httpRequestMessage, httpResponseMessage, statusCode));
            response.Append(ex);
        }

        return response;
    }

    internal static Task<HttpResponse> Execute(HttpClient httpClient, HttpRequestMessage httpRequestMessage, CancellationToken cancellationToken)
    {
        return Execute(httpClient, httpRequestMessage, HttpCompletionOption.ResponseContentRead, cancellationToken);
    }

    internal static Task<HttpResponse<T>> Execute<T>(HttpClient httpClient, HttpRequestMessage httpRequestMessage, JsonSerializerOptions jsonSerializerOptions, CancellationToken cancellationToken)
    {
        return Execute<T>(httpClient, httpRequestMessage, HttpCompletionOption.ResponseContentRead, jsonSerializerOptions, cancellationToken);
    }

    internal static Task<HttpResponse> Execute(HttpClient httpClient, HttpMethod httpMethod, string uri, CancellationToken cancellationToken)
    {
        return Execute(httpClient, new(httpMethod, uri), cancellationToken);
    }

    internal static Task<HttpResponse<T>> Execute<T>(HttpClient httpClient, HttpMethod httpMethod, string uri, JsonSerializerOptions jsonSerializerOptions, CancellationToken cancellationToken)
    {
        return Execute<T>(httpClient, new(httpMethod, uri), jsonSerializerOptions, cancellationToken);
    }

    internal static Task<HttpResponse> ExecuteWithContent(HttpClient httpClient, Stream contentStream, HttpMethod httpMethod, string uri, CancellationToken cancellationToken)
    {
        contentStream.Seek(0, SeekOrigin.Begin);

        StreamContent streamContent = new(contentStream);
        streamContent.Headers.ContentType = new("Application/json")
        {
            CharSet = Encoding.UTF8.WebName
        };
        HttpRequestMessage request = new(httpMethod, uri)
        {
            Content = streamContent
        };

        return Execute(httpClient, request, cancellationToken);
    }

    internal static Task<HttpResponse<T>> ExecuteWithContent<T>(HttpClient httpClient, Stream contentStream, HttpMethod httpMethod, string uri, JsonSerializerOptions jsonSerializerOptions, CancellationToken cancellationToken)
    {
        contentStream.Seek(0, SeekOrigin.Begin);

        StreamContent streamContent = new(contentStream);
        streamContent.Headers.ContentType = new("Application/json")
        {
            CharSet = Encoding.UTF8.WebName
        };
        HttpRequestMessage request = new(httpMethod, uri)
        {
            Content = streamContent
        };

        return Execute<T>(httpClient, request, jsonSerializerOptions, cancellationToken);
    }

    internal static Task<HttpResponse> ExecuteWithContent(HttpClient httpClient, string content, HttpMethod httpMethod, string uri, CancellationToken cancellationToken)
    {
        HttpRequestMessage request = new(httpMethod, uri)
        {
            Content = new StringContent(content, Encoding.UTF8, "Application/json")
        };

        return Execute(httpClient, request, cancellationToken);
    }

    internal static Task<HttpResponse<T>> ExecuteWithContent<T>(HttpClient httpClient, string content, HttpMethod httpMethod, string uri, JsonSerializerOptions jsonSerializerOptions, CancellationToken cancellationToken)
    {
        HttpRequestMessage request = new(httpMethod, uri)
        {
            Content = new StringContent(content, Encoding.UTF8, "Application/json")
        };

        return Execute<T>(httpClient, request, jsonSerializerOptions, cancellationToken);
    }
}
