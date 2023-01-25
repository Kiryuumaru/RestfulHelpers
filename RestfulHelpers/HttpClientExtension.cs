using System.IO;
using System.Net;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using RestfulHelpers.Common;

using static RestfulHelpers.Common.Internals.Message;

namespace RestfulHelpers;

/// <summary>
/// Helper extension for <see cref="HttpClient"/> REST API calls.
/// </summary>
public static class HttpClientExtension
{
    /// <summary>
    /// Executes an HTTP request and returns the response as an <see cref="HttpResponse"/> object.
    /// </summary>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="httpRequestMessage">The <see cref="HttpRequestMessage"/> representing the request.</param>
    /// <param name="httpCompletionOption">An <see cref="HttpCompletionOption"/> value that indicates when the operation should complete.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="HttpResponse"/> object representing the response to the request.</returns>
    public static async Task<HttpResponse> Execute(this HttpClient httpClient, HttpRequestMessage httpRequestMessage, HttpCompletionOption httpCompletionOption, CancellationToken cancellationToken)
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

    /// <summary>
    /// Executes an HTTP request and returns the response as a deserialized object of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of object to deserialize the response into.</typeparam>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="httpRequestMessage">The <see cref="HttpRequestMessage"/> representing the request.</param>
    /// <param name="httpCompletionOption">An <see cref="HttpCompletionOption"/> value that indicates when the operation should complete.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use when deserializing the response.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="HttpResponse{T}"/> object representing the response to the request and the deserialized object.</returns>
    /// <exception cref="JsonException">
    /// The JSON is invalid.
    ///
    /// -or-
    ///
    /// <typeparamref name="T" /> is not compatible with the JSON.
    ///
    /// -or-
    ///
    /// There is remaining data in the string beyond a single JSON value.</exception>
    /// <exception cref="NotSupportedException">
    /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
    /// for <typeparamref name="T"/> or its serializable members.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    public static async Task<HttpResponse<T>> Execute<T>(this HttpClient httpClient, HttpRequestMessage httpRequestMessage, HttpCompletionOption httpCompletionOption, JsonSerializerOptions jsonSerializerOptions, CancellationToken cancellationToken)
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

    /// <summary>
    /// Executes an HTTP request and returns the response as an <see cref="HttpResponse"/> object using default <see cref="HttpCompletionOption"/>.
    /// </summary>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="httpRequestMessage">The <see cref="HttpRequestMessage"/> representing the request.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="HttpResponse"/> object representing the response to the request.</returns>
    public static Task<HttpResponse> Execute(this HttpClient httpClient, HttpRequestMessage httpRequestMessage, CancellationToken cancellationToken)
    {
        return Execute(httpClient, httpRequestMessage, HttpCompletionOption.ResponseContentRead, cancellationToken);
    }

    /// <summary>
    /// Executes an HTTP request and returns the response as a deserialized object of the specified type using default <see cref="HttpCompletionOption"/>.
    /// </summary>
    /// <typeparam name="T">The type of object to deserialize the response into.</typeparam>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="httpRequestMessage">The <see cref="HttpRequestMessage"/> representing the request.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use when deserializing the response.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="HttpResponse{T}"/> object representing the response to the request and the deserialized object.</returns>
    /// <exception cref="JsonException">
    /// The JSON is invalid.
    ///
    /// -or-
    ///
    /// <typeparamref name="T" /> is not compatible with the JSON.
    ///
    /// -or-
    ///
    /// There is remaining data in the string beyond a single JSON value.</exception>
    /// <exception cref="NotSupportedException">
    /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
    /// for <typeparamref name="T"/> or its serializable members.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    public static Task<HttpResponse<T>> Execute<T>(this HttpClient httpClient, HttpRequestMessage httpRequestMessage, JsonSerializerOptions jsonSerializerOptions, CancellationToken cancellationToken)
    {
        return Execute<T>(httpClient, httpRequestMessage, HttpCompletionOption.ResponseContentRead, jsonSerializerOptions, cancellationToken);
    }

    /// <summary>
    /// Executes an HTTP request and returns the response as an <see cref="HttpResponse"/> object using the specified <see cref="HttpMethod"/> and URI.
    /// </summary>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="httpMethod">The <see cref="HttpMethod"/> representing the request.</param>
    /// <param name="uri">The URI of the request.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="HttpResponse"/> object representing the response to the request.</returns>
    public static Task<HttpResponse> Execute(this HttpClient httpClient, HttpMethod httpMethod, string uri, CancellationToken cancellationToken)
    {
        return Execute(httpClient, new(httpMethod, uri), cancellationToken);
    }

    /// <summary>
    /// Executes an HTTP request and returns the response as a deserialized object of the specified type using the specified <see cref="HttpMethod"/> and URI.
    /// </summary>
    /// <typeparam name="T">The type of object to deserialize the response into.</typeparam>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="httpMethod">The <see cref="HttpMethod"/> representing the request.</param>
    /// <param name="uri">The URI of the request.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use when deserializing the response.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="HttpResponse{T}"/> object representing the response to the request and the deserialized object.</returns>
    /// <exception cref="JsonException">
    /// The JSON is invalid.
    ///
    /// -or-
    ///
    /// <typeparamref name="T" /> is not compatible with the JSON.
    ///
    /// -or-
    ///
    /// There is remaining data in the string beyond a single JSON value.</exception>
    /// <exception cref="NotSupportedException">
    /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
    /// for <typeparamref name="T"/> or its serializable members.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    public static Task<HttpResponse<T>> Execute<T>(this HttpClient httpClient, HttpMethod httpMethod, string uri, JsonSerializerOptions jsonSerializerOptions, CancellationToken cancellationToken)
    {
        return Execute<T>(httpClient, new(httpMethod, uri), jsonSerializerOptions, cancellationToken);
    }

    /// <summary>
    /// Executes an HTTP request with a provided <see cref="Stream"/> as the content and returns the response as an <see cref="HttpResponse"/> object.
    /// </summary>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="contentStream">The <see cref="Stream"/> representing the content of the request.</param>
    /// <param name="httpMethod">The <see cref="HttpMethod"/> representing the request.</param>
    /// <param name="uri">The URI of the request.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="HttpResponse"/> object representing the response to the request.</returns>
    public static Task<HttpResponse> ExecuteWithContent(this HttpClient httpClient, Stream contentStream, HttpMethod httpMethod, string uri, CancellationToken cancellationToken)
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

    /// <summary>
    /// Executes an HTTP request with a provided <see cref="Stream"/> as the content and returns the response as a deserialized object of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of object to deserialize the response into.</typeparam>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="contentStream">The <see cref="Stream"/> representing the content of the request.</param>
    /// <param name="httpMethod">The <see cref="HttpMethod"/> representing the request.</param>
    /// <param name="uri">The URI of the request.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use when deserializing the response.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="HttpResponse{T}"/> object representing the response to the request and the deserialized object.</returns>
    /// <exception cref="JsonException">
    /// The JSON is invalid.
    ///
    /// -or-
    ///
    /// <typeparamref name="T" /> is not compatible with the JSON.
    ///
    /// -or-
    ///
    /// There is remaining data in the string beyond a single JSON value.</exception>
    /// <exception cref="NotSupportedException">
    /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
    /// for <typeparamref name="T"/> or its serializable members.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    public static Task<HttpResponse<T>> ExecuteWithContent<T>(this HttpClient httpClient, Stream contentStream, HttpMethod httpMethod, string uri, JsonSerializerOptions jsonSerializerOptions, CancellationToken cancellationToken)
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

    /// <summary>
    /// Executes an HTTP request with a provided string as the content and returns the response as an <see cref="HttpResponse"/> object.
    /// </summary>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="content">The string representing the content of the request.</param>
    /// <param name="httpMethod">The <see cref="HttpMethod"/> representing the request.</param>
    /// <param name="uri">The URI of the request.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="HttpResponse"/> object representing the response to the request.</returns>
    public static Task<HttpResponse> ExecuteWithContent(this HttpClient httpClient, string content, HttpMethod httpMethod, string uri, CancellationToken cancellationToken)
    {
        HttpRequestMessage request = new(httpMethod, uri)
        {
            Content = new StringContent(content, Encoding.UTF8, "Application/json")
        };

        return Execute(httpClient, request, cancellationToken);
    }

    /// <summary>
    /// Executes an HTTP request with a provided string as the content and returns the response as a deserialized object of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of object to deserialize the response into.</typeparam>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="content">The string representing the content of the request.</param>
    /// <param name="httpMethod">The <see cref="HttpMethod"/> representing the request.</param>
    /// <param name="uri">The URI of the request.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use when deserializing the response.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="HttpResponse{T}"/> object representing the response to the request and the deserialized object.</returns>
    /// <exception cref="JsonException">
    /// The JSON is invalid.
    ///
    /// -or-
    ///
    /// <typeparamref name="T" /> is not compatible with the JSON.
    ///
    /// -or-
    ///
    /// There is remaining data in the string beyond a single JSON value.</exception>
    /// <exception cref="NotSupportedException">
    /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
    /// for <typeparamref name="T"/> or its serializable members.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    public static Task<HttpResponse<T>> ExecuteWithContent<T>(this HttpClient httpClient, string content, HttpMethod httpMethod, string uri, JsonSerializerOptions jsonSerializerOptions, CancellationToken cancellationToken)
    {
        HttpRequestMessage request = new(httpMethod, uri)
        {
            Content = new StringContent(content, Encoding.UTF8, "Application/json")
        };

        return Execute<T>(httpClient, request, jsonSerializerOptions, cancellationToken);
    }
}
