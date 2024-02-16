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
using TransactionHelpers;

using static RestfulHelpers.Common.Internals.Message;

namespace RestfulHelpers;

/// <summary>
/// Helper extension for <see cref="HttpClient"/> REST API calls.
/// </summary>
public static class HttpClientExtension
{
    /// <summary>
    /// Executes an HTTP request and returns the result as an <see cref="HttpResult"/> object.
    /// </summary>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="httpRequestMessage">The <see cref="HttpRequestMessage"/> representing the request.</param>
    /// <param name="httpCompletionOption">An <see cref="HttpCompletionOption"/> value that indicates when the operation should complete.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="Result"/> object representing the result to the request.</returns>
    public static async Task<HttpResult> Execute(this HttpClient httpClient, HttpRequestMessage httpRequestMessage, HttpCompletionOption httpCompletionOption, CancellationToken cancellationToken = default)
    {
        HttpResult result = new();
        HttpStatusCode statusCode = HttpStatusCode.OK;

        if (httpRequestMessage.Content != null)
        {
            await httpRequestMessage.Content.LoadIntoBufferAsync();
        }

        try
        {
            var httpResultMessage = await httpClient.SendAsync(httpRequestMessage, httpCompletionOption, cancellationToken);

            statusCode = httpResultMessage.StatusCode;

            httpResultMessage.EnsureSuccessStatusCode();

            result.WithStatusCode(statusCode);
        }
        catch (Exception ex)
        {
            result
                .WithError(ex)
                .WithStatusCode(statusCode);
        }

        return result;
    }

    /// <summary>
    /// Executes an HTTP request and returns the result as a deserialized object of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of object to deserialize the result into.</typeparam>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="httpRequestMessage">The <see cref="HttpRequestMessage"/> representing the request.</param>
    /// <param name="httpCompletionOption">An <see cref="HttpCompletionOption"/> value that indicates when the operation should complete.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use when deserializing the result.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="HttpResult{T}"/> object representing the result to the request and the deserialized object.</returns>
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
    public static async Task<HttpResult<T>> Execute<T>(this HttpClient httpClient, HttpRequestMessage httpRequestMessage, HttpCompletionOption httpCompletionOption, JsonSerializerOptions? jsonSerializerOptions = default, CancellationToken cancellationToken = default)
    {
        HttpResult<T> result = new();
        HttpStatusCode statusCode = HttpStatusCode.OK;

        if (httpRequestMessage.Content != null)
        {
            await httpRequestMessage.Content.LoadIntoBufferAsync();
        }

        try
        {
            var httpResultMessage = await httpClient.SendAsync(httpRequestMessage, httpCompletionOption, cancellationToken);

            statusCode = httpResultMessage.StatusCode;

            httpResultMessage.EnsureSuccessStatusCode();

#if NETSTANDARD
            var httpResultMessageData = await httpResultMessage.Content.ReadAsStringAsync();
#else
            var httpResultMessageData = await httpResultMessage.Content.ReadAsStringAsync(cancellationToken);
#endif

            result
                .WithValue(JsonSerializer.Deserialize<T>(httpResultMessageData, jsonSerializerOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web)))
                .WithStatusCode(statusCode);
        }
        catch (Exception ex)
        {
            result
                .WithError(ex)
                .WithStatusCode(statusCode);
        }

        return result;
    }

    /// <summary>
    /// Executes an HTTP request and returns the result as an <see cref="HttpResult"/> object using default <see cref="HttpCompletionOption"/>.
    /// </summary>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="httpRequestMessage">The <see cref="HttpRequestMessage"/> representing the request.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="HttpResult"/> object representing the result to the request.</returns>
    public static Task<HttpResult> Execute(this HttpClient httpClient, HttpRequestMessage httpRequestMessage, CancellationToken cancellationToken = default)
    {
        return Execute(httpClient, httpRequestMessage, HttpCompletionOption.ResponseContentRead, cancellationToken);
    }

    /// <summary>
    /// Executes an HTTP request and returns the result as a deserialized object of the specified type using default <see cref="HttpCompletionOption"/>.
    /// </summary>
    /// <typeparam name="T">The type of object to deserialize the result into.</typeparam>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="httpRequestMessage">The <see cref="HttpRequestMessage"/> representing the request.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use when deserializing the result.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="HttpResult{T}"/> object representing the result to the request and the deserialized object.</returns>
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
    public static Task<HttpResult<T>> Execute<T>(this HttpClient httpClient, HttpRequestMessage httpRequestMessage, JsonSerializerOptions? jsonSerializerOptions = default, CancellationToken cancellationToken = default)
    {
        return Execute<T>(httpClient, httpRequestMessage, HttpCompletionOption.ResponseContentRead, jsonSerializerOptions, cancellationToken);
    }

    /// <summary>
    /// Executes an HTTP request and returns the result as an <see cref="HttpResult"/> object using the specified <see cref="HttpMethod"/> and URI.
    /// </summary>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="httpMethod">The <see cref="HttpMethod"/> representing the request.</param>
    /// <param name="uri">The URI of the request.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="HttpResult"/> object representing the result to the request.</returns>
    public static Task<HttpResult> Execute(this HttpClient httpClient, HttpMethod httpMethod, Uri uri, CancellationToken cancellationToken = default)
    {
        return Execute(httpClient, new(httpMethod, uri), cancellationToken);
    }

    /// <summary>
    /// Executes an HTTP request and returns the result as an <see cref="HttpResult"/> object using the specified <see cref="HttpMethod"/> and URI.
    /// </summary>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="httpMethod">The <see cref="HttpMethod"/> representing the request.</param>
    /// <param name="uri">The URI of the request.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="HttpResult"/> object representing the result to the request.</returns>
    public static Task<HttpResult> Execute(this HttpClient httpClient, HttpMethod httpMethod, string uri, CancellationToken cancellationToken = default)
    {
        return Execute(httpClient, httpMethod, new Uri(uri), cancellationToken);
    }

    /// <summary>
    /// Executes an HTTP request and returns the result as a deserialized object of the specified type using the specified <see cref="HttpMethod"/> and URI.
    /// </summary>
    /// <typeparam name="T">The type of object to deserialize the result into.</typeparam>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="httpMethod">The <see cref="HttpMethod"/> representing the request.</param>
    /// <param name="uri">The URI of the request.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use when deserializing the result.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="HttpResult{T}"/> object representing the result to the request and the deserialized object.</returns>
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
    public static Task<HttpResult<T>> Execute<T>(this HttpClient httpClient, HttpMethod httpMethod, Uri uri, JsonSerializerOptions? jsonSerializerOptions = default, CancellationToken cancellationToken = default)
    {
        return Execute<T>(httpClient, new(httpMethod, uri), jsonSerializerOptions, cancellationToken);
    }

    /// <summary>
    /// Executes an HTTP request and returns the result as a deserialized object of the specified type using the specified <see cref="HttpMethod"/> and URI.
    /// </summary>
    /// <typeparam name="T">The type of object to deserialize the result into.</typeparam>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="httpMethod">The <see cref="HttpMethod"/> representing the request.</param>
    /// <param name="uri">The URI of the request.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use when deserializing the result.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="HttpResult{T}"/> object representing the result to the request and the deserialized object.</returns>
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
    public static Task<HttpResult<T>> Execute<T>(this HttpClient httpClient, HttpMethod httpMethod, string uri, JsonSerializerOptions? jsonSerializerOptions = default, CancellationToken cancellationToken = default)
    {
        return Execute<T>(httpClient, httpMethod, new Uri(uri), jsonSerializerOptions, cancellationToken);
    }

    /// <summary>
    /// Executes an HTTP request with a provided <see cref="Stream"/> as the content and returns the result as an <see cref="HttpResult"/> object.
    /// </summary>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="contentStream">The <see cref="Stream"/> representing the content of the request.</param>
    /// <param name="httpMethod">The <see cref="HttpMethod"/> representing the request.</param>
    /// <param name="uri">The URI of the request.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="HttpResult"/> object representing the result to the request.</returns>
    public static Task<HttpResult> ExecuteWithContent(this HttpClient httpClient, Stream contentStream, HttpMethod httpMethod, Uri uri, CancellationToken cancellationToken = default)
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
    /// Executes an HTTP request with a provided <see cref="Stream"/> as the content and returns the result as an <see cref="HttpResult"/> object.
    /// </summary>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="contentStream">The <see cref="Stream"/> representing the content of the request.</param>
    /// <param name="httpMethod">The <see cref="HttpMethod"/> representing the request.</param>
    /// <param name="uri">The URI of the request.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="HttpResult"/> object representing the result to the request.</returns>
    public static Task<HttpResult> ExecuteWithContent(this HttpClient httpClient, Stream contentStream, HttpMethod httpMethod, string uri, CancellationToken cancellationToken = default)
    {
        return ExecuteWithContent(httpClient, contentStream, httpMethod, new Uri(uri), cancellationToken);
    }

    /// <summary>
    /// Executes an HTTP request with a provided <see cref="Stream"/> as the content and returns the result as a deserialized object of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of object to deserialize the result into.</typeparam>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="contentStream">The <see cref="Stream"/> representing the content of the request.</param>
    /// <param name="httpMethod">The <see cref="HttpMethod"/> representing the request.</param>
    /// <param name="uri">The URI of the request.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use when deserializing the result.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="HttpResult{T}"/> object representing the result to the request and the deserialized object.</returns>
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
    public static Task<HttpResult<T>> ExecuteWithContent<T>(this HttpClient httpClient, Stream contentStream, HttpMethod httpMethod, Uri uri, JsonSerializerOptions? jsonSerializerOptions = default, CancellationToken cancellationToken = default)
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
    /// Executes an HTTP request with a provided <see cref="Stream"/> as the content and returns the result as a deserialized object of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of object to deserialize the result into.</typeparam>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="contentStream">The <see cref="Stream"/> representing the content of the request.</param>
    /// <param name="httpMethod">The <see cref="HttpMethod"/> representing the request.</param>
    /// <param name="uri">The URI of the request.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use when deserializing the result.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="HttpResult{T}"/> object representing the result to the request and the deserialized object.</returns>
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
    public static Task<HttpResult<T>> ExecuteWithContent<T>(this HttpClient httpClient, Stream contentStream, HttpMethod httpMethod, string uri, JsonSerializerOptions? jsonSerializerOptions = default, CancellationToken cancellationToken = default)
    {
        return ExecuteWithContent<T>(httpClient, contentStream, httpMethod, new Uri(uri), jsonSerializerOptions, cancellationToken);
    }

    /// <summary>
    /// Executes an HTTP request with a provided string as the content and returns the result as an <see cref="HttpResult"/> object.
    /// </summary>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="content">The string representing the content of the request.</param>
    /// <param name="httpMethod">The <see cref="HttpMethod"/> representing the request.</param>
    /// <param name="uri">The URI of the request.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="HttpResult"/> object representing the result to the request.</returns>
    public static Task<HttpResult> ExecuteWithContent(this HttpClient httpClient, string content, HttpMethod httpMethod, Uri uri, CancellationToken cancellationToken = default)
    {
        HttpRequestMessage request = new(httpMethod, uri)
        {
            Content = new StringContent(content, Encoding.UTF8, "Application/json")
        };

        return Execute(httpClient, request, cancellationToken);
    }

    /// <summary>
    /// Executes an HTTP request with a provided string as the content and returns the result as an <see cref="HttpResult"/> object.
    /// </summary>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="content">The string representing the content of the request.</param>
    /// <param name="httpMethod">The <see cref="HttpMethod"/> representing the request.</param>
    /// <param name="uri">The URI of the request.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="HttpResult"/> object representing the result to the request.</returns>
    public static Task<HttpResult> ExecuteWithContent(this HttpClient httpClient, string content, HttpMethod httpMethod, string uri, CancellationToken cancellationToken = default)
    {
        return ExecuteWithContent(httpClient, content, httpMethod, new Uri(uri), cancellationToken);
    }

    /// <summary>
    /// Executes an HTTP request with a provided string as the content and returns the result as a deserialized object of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of object to deserialize the result into.</typeparam>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="content">The string representing the content of the request.</param>
    /// <param name="httpMethod">The <see cref="HttpMethod"/> representing the request.</param>
    /// <param name="uri">The URI of the request.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use when deserializing the result.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="HttpResult{T}"/> object representing the result to the request and the deserialized object.</returns>
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
    public static Task<HttpResult<T>> ExecuteWithContent<T>(this HttpClient httpClient, string content, HttpMethod httpMethod, Uri uri, JsonSerializerOptions? jsonSerializerOptions = default, CancellationToken cancellationToken = default)
    {
        HttpRequestMessage request = new(httpMethod, uri)
        {
            Content = new StringContent(content, Encoding.UTF8, "Application/json")
        };

        return Execute<T>(httpClient, request, jsonSerializerOptions, cancellationToken);
    }

    /// <summary>
    /// Executes an HTTP request with a provided string as the content and returns the result as a deserialized object of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of object to deserialize the result into.</typeparam>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="content">The string representing the content of the request.</param>
    /// <param name="httpMethod">The <see cref="HttpMethod"/> representing the request.</param>
    /// <param name="uri">The URI of the request.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use when deserializing the result.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="HttpResult{T}"/> object representing the result to the request and the deserialized object.</returns>
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
    public static Task<HttpResult<T>> ExecuteWithContent<T>(this HttpClient httpClient, string content, HttpMethod httpMethod, string uri, JsonSerializerOptions? jsonSerializerOptions = default, CancellationToken cancellationToken = default)
    {
        return ExecuteWithContent<T>(httpClient, content, httpMethod, new Uri(uri), jsonSerializerOptions, cancellationToken);
    }

    /// <summary>
    /// Executes an HTTP request with a provided <typeparamref name="TContent"/> as the content and returns the result as an <see cref="HttpResult"/> object.
    /// </summary>
    /// <typeparam name="TContent">The type of content to serialize.</typeparam>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="content">The string representing the content of the request.</param>
    /// <param name="httpMethod">The <see cref="HttpMethod"/> representing the request.</param>
    /// <param name="uri">The URI of the request.</param>
    /// <param name="jsonSerializerOptions">A <see cref="JsonSerializerOptions"/> that can be used to serialize <paramref name="content"/>.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="HttpResult"/> object representing the result to the request.</returns>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    public static Task<HttpResult> ExecuteWithContent<TContent>(this HttpClient httpClient, TContent content, HttpMethod httpMethod, Uri uri, JsonSerializerOptions? jsonSerializerOptions = default, CancellationToken cancellationToken = default)
    {
        HttpRequestMessage request = new(httpMethod, uri)
        {
            Content = new StringContent(JsonSerializer.Serialize(content, jsonSerializerOptions), Encoding.UTF8, "Application/json")
        };

        return Execute(httpClient, request, cancellationToken);
    }

    /// <summary>
    /// Executes an HTTP request with a provided string as the content and returns the result as an <see cref="HttpResult"/> object.
    /// </summary>
    /// <typeparam name="TContent">The type of content to serialize.</typeparam>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="content">The string representing the content of the request.</param>
    /// <param name="httpMethod">The <see cref="HttpMethod"/> representing the request.</param>
    /// <param name="uri">The URI of the request.</param>
    /// <param name="jsonSerializerOptions">A <see cref="JsonSerializerOptions"/> that can be used to serialize <paramref name="content"/>.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="HttpResult"/> object representing the result to the request.</returns>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    public static Task<HttpResult> ExecuteWithContent<TContent>(this HttpClient httpClient, TContent content, HttpMethod httpMethod, string uri, JsonSerializerOptions? jsonSerializerOptions = default, CancellationToken cancellationToken = default)
    {
        return ExecuteWithContent(httpClient, JsonSerializer.Serialize(content, jsonSerializerOptions), httpMethod, new Uri(uri), cancellationToken);
    }

    /// <summary>
    /// Executes an HTTP request with a provided <typeparamref name="TContent"/> as the content and returns the result as a deserialized object of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of object to deserialize the result into.</typeparam>
    /// <typeparam name="TContent">The type of content to serialize.</typeparam>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="content">The string representing the content of the request.</param>
    /// <param name="httpMethod">The <see cref="HttpMethod"/> representing the request.</param>
    /// <param name="uri">The URI of the request.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use when deserializing the result.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="HttpResult{T}"/> object representing the result to the request and the deserialized object.</returns>
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
    public static Task<HttpResult<T>> ExecuteWithContent<T, TContent>(this HttpClient httpClient, TContent content, HttpMethod httpMethod, Uri uri, JsonSerializerOptions? jsonSerializerOptions = default, CancellationToken cancellationToken = default)
    {
        HttpRequestMessage request = new(httpMethod, uri)
        {
            Content = new StringContent(JsonSerializer.Serialize(content, jsonSerializerOptions), Encoding.UTF8, "Application/json")
        };

        return Execute<T>(httpClient, request, jsonSerializerOptions, cancellationToken);
    }

    /// <summary>
    /// Executes an HTTP request with a provided <typeparamref name="TContent"/> as the content and returns the result as a deserialized object of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of object to deserialize the result into.</typeparam>
    /// <typeparam name="TContent">The type of content to serialize.</typeparam>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="content">The string representing the content of the request.</param>
    /// <param name="httpMethod">The <see cref="HttpMethod"/> representing the request.</param>
    /// <param name="uri">The URI of the request.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use when deserializing the result.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="HttpResult{T}"/> object representing the result to the request and the deserialized object.</returns>
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
    public static Task<HttpResult<T>> ExecuteWithContent<T, TContent>(this HttpClient httpClient, TContent content, HttpMethod httpMethod, string uri, JsonSerializerOptions? jsonSerializerOptions = default, CancellationToken cancellationToken = default)
    {
        return ExecuteWithContent<T, TContent>(httpClient, content, httpMethod, new Uri(uri), jsonSerializerOptions, cancellationToken);
    }
}
