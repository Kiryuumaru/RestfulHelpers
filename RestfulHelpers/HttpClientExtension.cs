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
using RestfulHelpers.Interface;
using TransactionHelpers;
using TransactionHelpers.Interface;
using System.Linq;
using System.Text.Json.Serialization.Metadata;
using System.Text.Json.Serialization;
using System.Collections.Generic;

using static RestfulHelpers.Common.Internals.Message;
using System.Xml.Linq;

namespace RestfulHelpers;

/// <summary>
/// Helper extension for <see cref="HttpClient"/> REST API calls.
/// </summary>
public static class HttpClientExtension
{
#if NET7_0_OR_GREATER
    private static bool VerifyCascade(IHttpResult result, string httpResultMessageData, HttpStatusCode statusCode, JsonDocument? doc, RestfulHelpersJsonSerializerContext jsonSerializerContext, Action<JsonElement>? onValueElement)
#else
    private static bool VerifyCascade(IHttpResult result, string httpResultMessageData, HttpStatusCode statusCode, JsonDocument? doc, JsonSerializerOptions jsonSerializerOptions, Action<JsonElement>? onValueElement)
#endif
    {
        try
        {
            if (doc != null && doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.EnumerateObject().All(i =>
                    i.Name.Equals("value", StringComparison.InvariantCultureIgnoreCase) ||
                    i.Name.Equals("hasvalue", StringComparison.InvariantCultureIgnoreCase) ||
                    i.Name.Equals("errors", StringComparison.InvariantCultureIgnoreCase) ||
                    i.Name.Equals("issuccess", StringComparison.InvariantCultureIgnoreCase) ||
                    i.Name.Equals("statuscode", StringComparison.InvariantCultureIgnoreCase)))
            {
                bool hasStatusCode = false;
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (prop.Name.Equals("value", StringComparison.InvariantCultureIgnoreCase))
                    {
                        onValueElement?.Invoke(prop.Value);
                    }
                    if (prop.Name.Equals("errors", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (prop.Value.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var errorElement in prop.Value.EnumerateArray())
                            {
                                if (errorElement.EnumerateObject().All(i =>
                                        i.Name.Equals("message", StringComparison.InvariantCultureIgnoreCase) ||
                                        i.Name.Equals("code", StringComparison.InvariantCultureIgnoreCase) ||
                                        i.Name.Equals("detail", StringComparison.InvariantCultureIgnoreCase)))
                                {
                                    bool isHttpError = false;

                                    var detailProperty = errorElement
                                        .EnumerateObject()
                                        .FirstOrDefault(p => string.Compare(p.Name, "detail", StringComparison.InvariantCultureIgnoreCase) == 0)
                                        .Value;

                                    if (detailProperty.ValueKind == JsonValueKind.Object)
                                    {
                                        var statucCodeProperty = detailProperty
                                            .EnumerateObject()
                                            .FirstOrDefault(p => string.Compare(p.Name, "status", StringComparison.InvariantCultureIgnoreCase) == 0);

                                        if (statucCodeProperty.Value.ValueKind == JsonValueKind.Number)
                                        {
                                            isHttpError = true;
                                        }
                                    }

                                    if (isHttpError)
                                    {
#if NET7_0_OR_GREATER
                                        var error = errorElement.Deserialize(jsonSerializerContext.HttpError);
#else
                                        var error = errorElement.Deserialize<HttpError>(jsonSerializerOptions);
#endif
                                        result.WithError(error);
                                    }
                                    else
                                    {
#if NET7_0_OR_GREATER
                                        var error = errorElement.Deserialize(jsonSerializerContext.Error);
#else
                                        var error = errorElement.Deserialize<Error>(jsonSerializerOptions);
#endif
                                        result.WithError(error);
                                    }
                                }
                            }
                        }
                    }
                    if (prop.Name.Equals("statuscode", StringComparison.InvariantCultureIgnoreCase))
                    {
                        hasStatusCode = true;
                        result.InternalStatusCode = (HttpStatusCode)prop.Value.GetInt32();
                    }
                }

                if (!hasStatusCode)
                {
                    result.InternalStatusCode = statusCode;
                }

                return true;
            }
        }
        catch
        {
            result
                .WithError(new JsonException($"Result is not in json format: {httpResultMessageData}"));

            result.InternalStatusCode = statusCode;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Executes an HTTP request and returns the result as an <see cref="HttpResult"/> object.
    /// </summary>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="httpRequestMessage">The <see cref="HttpRequestMessage"/> representing the request.</param>
    /// <param name="httpCompletionOption">An <see cref="HttpCompletionOption"/> value that indicates when the operation should complete.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use when deserializing the result.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="Result"/> object representing the result to the request.</returns>
    public static async Task<HttpResult> Execute(this HttpClient httpClient, HttpRequestMessage httpRequestMessage, HttpCompletionOption httpCompletionOption, JsonSerializerOptions? jsonSerializerOptions = default, CancellationToken cancellationToken = default)
    {
        HttpResult result = new();
        HttpStatusCode statusCode = HttpStatusCode.OK;

        jsonSerializerOptions ??= new JsonSerializerOptions(JsonSerializerDefaults.Web);
#if NET7_0_OR_GREATER
        var jsonSerializerContext = new RestfulHelpersJsonSerializerContext(new JsonSerializerOptions(jsonSerializerOptions));
#endif
        JsonDocumentOptions jsonDocumentOptions = new()
        {
            AllowTrailingCommas = jsonSerializerOptions.AllowTrailingCommas,
            CommentHandling = jsonSerializerOptions.ReadCommentHandling,
            MaxDepth = jsonSerializerOptions.MaxDepth,
        };

        try
        {
            var httpResultMessage = await httpClient.SendAsync(httpRequestMessage, httpCompletionOption, cancellationToken);

            var headers = httpResultMessage.Headers.Concat(httpResultMessage.Content.Headers);
            (result as IHttpResult).InternalResponseHeaders = headers.ToDictionary(h => h.Key, h => h.Value.ToArray());

            statusCode = httpResultMessage.StatusCode;

#if NETSTANDARD
            var httpResultMessageData = await httpResultMessage.Content.ReadAsStringAsync();
#else
            var httpResultMessageData = await httpResultMessage.Content.ReadAsStringAsync(cancellationToken);
#endif
            JsonDocument? doc = null;
            try
            {
                doc = JsonDocument.Parse(httpResultMessageData, jsonDocumentOptions);
            }
            catch { }

#if NET7_0_OR_GREATER
            if (VerifyCascade(result, httpResultMessageData, statusCode, doc, jsonSerializerContext, null))
#else
            if (VerifyCascade(result, httpResultMessageData, statusCode, doc, jsonSerializerOptions, null))
#endif
            {
                return result;
            }

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

#if NET7_0_OR_GREATER
    /// <summary>
    /// Executes an HTTP request and returns the result as a deserialized object of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of object to deserialize the result into.</typeparam>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="httpRequestMessage">The <see cref="HttpRequestMessage"/> representing the request.</param>
    /// <param name="httpCompletionOption">An <see cref="HttpCompletionOption"/> value that indicates when the operation should complete.</param>
    /// <param name="jsonTypeInfo">The <see cref="JsonTypeInfo"/> metadata to use when deserializing the result.</param>
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
    /// There is no compatible <see cref="JsonConverter"/>
    /// for <typeparamref name="T"/> or its serializable members.
    /// </exception>
    public static async Task<HttpResult<T>> Execute<T>(this HttpClient httpClient, HttpRequestMessage httpRequestMessage, HttpCompletionOption httpCompletionOption, JsonTypeInfo<T> jsonTypeInfo, CancellationToken cancellationToken = default)
    {
        HttpResult<T> result = new();
        HttpStatusCode statusCode = HttpStatusCode.OK;

        var jsonSerializerContext = new RestfulHelpersJsonSerializerContext(new JsonSerializerOptions(jsonTypeInfo.Options));
        JsonDocumentOptions jsonDocumentOptions = new()
        {
            AllowTrailingCommas = jsonTypeInfo.Options.AllowTrailingCommas,
            CommentHandling = jsonTypeInfo.Options.ReadCommentHandling,
            MaxDepth = jsonTypeInfo.Options.MaxDepth,
        };

        try
        {
            var httpResultMessage = await httpClient.SendAsync(httpRequestMessage, httpCompletionOption, cancellationToken);

            var headers = httpResultMessage.Headers.Concat(httpResultMessage.Content.Headers);
            (result as IHttpResult).InternalResponseHeaders = headers.ToDictionary(h => h.Key, h => h.Value.ToArray());

            statusCode = httpResultMessage.StatusCode;

#if NETSTANDARD
            var httpResultMessageData = await httpResultMessage.Content.ReadAsStringAsync();
#else
            var httpResultMessageData = await httpResultMessage.Content.ReadAsStringAsync(cancellationToken);
#endif
            JsonDocument? doc = null;
            try
            {
                doc = JsonDocument.Parse(httpResultMessageData, jsonDocumentOptions);
            }
            catch { }

            void valueSetter(JsonElement valueElement)
            {
                result.WithValue(valueElement.Deserialize(jsonTypeInfo));
            }

#if NET7_0_OR_GREATER
            if (VerifyCascade(result, httpResultMessageData, statusCode, doc, jsonSerializerContext, valueSetter))
#else
            if (VerifyCascade(result, httpResultMessageData, statusCode, doc, jsonSerializerOptions, valueSetter))
#endif
            {
                return result;
            }

            if (doc != null)
            {
                valueSetter(doc.RootElement);
            }

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
#endif

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
    /// There is no compatible <see cref="JsonConverter"/>
    /// for <typeparamref name="T"/> or its serializable members.
    /// </exception>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode(RequiresDynamicCode)]
#endif
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    public static async Task<HttpResult<T>> Execute<T>(this HttpClient httpClient, HttpRequestMessage httpRequestMessage, HttpCompletionOption httpCompletionOption, JsonSerializerOptions? jsonSerializerOptions = default, CancellationToken cancellationToken = default)
    {
        HttpResult<T> result = new();
        HttpStatusCode statusCode = HttpStatusCode.OK;

        jsonSerializerOptions ??= new JsonSerializerOptions(JsonSerializerDefaults.Web);
        JsonDocumentOptions jsonDocumentOptions = new()
        {
            AllowTrailingCommas = jsonSerializerOptions.AllowTrailingCommas,
            CommentHandling = jsonSerializerOptions.ReadCommentHandling,
            MaxDepth = jsonSerializerOptions.MaxDepth,
        };
#if NET7_0_OR_GREATER
        var jsonSerializerContext = new RestfulHelpersJsonSerializerContext(new JsonSerializerOptions(jsonSerializerOptions));
#endif

        try
        {
            var httpResultMessage = await httpClient.SendAsync(httpRequestMessage, httpCompletionOption, cancellationToken);

            var headers = httpResultMessage.Headers.Concat(httpResultMessage.Content.Headers);
            (result as IHttpResult).InternalResponseHeaders = headers.ToDictionary(h => h.Key, h => h.Value.ToArray());

            statusCode = httpResultMessage.StatusCode;

#if NETSTANDARD
            var httpResultMessageData = await httpResultMessage.Content.ReadAsStringAsync();
#else
            var httpResultMessageData = await httpResultMessage.Content.ReadAsStringAsync(cancellationToken);
#endif
            JsonDocument? doc = null;
            try
            {
                doc = JsonDocument.Parse(httpResultMessageData, jsonDocumentOptions);
            }
            catch { }

            void valueSetter(JsonElement valueElement)
            {
                result.WithValue(valueElement.Deserialize<T>(jsonSerializerOptions));
            }

#if NET7_0_OR_GREATER
            if (VerifyCascade(result, httpResultMessageData, statusCode, doc, jsonSerializerContext, valueSetter))
#else
            if (VerifyCascade(result, httpResultMessageData, statusCode, doc, jsonSerializerOptions, valueSetter))
#endif
            {
                return result;
            }

            if (doc != null)
            {
                valueSetter(doc.RootElement);
            }

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
    /// Executes an HTTP request and returns the result as an <see cref="HttpResult"/> object using default <see cref="HttpCompletionOption"/>.
    /// </summary>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="httpRequestMessage">The <see cref="HttpRequestMessage"/> representing the request.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use when deserializing the result.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="HttpResult"/> object representing the result to the request.</returns>
    public static Task<HttpResult> Execute(this HttpClient httpClient, HttpRequestMessage httpRequestMessage, JsonSerializerOptions? jsonSerializerOptions = default, CancellationToken cancellationToken = default)
    {
        return Execute(httpClient, httpRequestMessage, HttpCompletionOption.ResponseContentRead, jsonSerializerOptions, cancellationToken);
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
    /// There is no compatible <see cref="JsonConverter"/>
    /// for <typeparamref name="T"/> or its serializable members.
    /// </exception>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode(RequiresDynamicCode)]
#endif
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    public static Task<HttpResult<T>> Execute<T>(this HttpClient httpClient, HttpRequestMessage httpRequestMessage, JsonSerializerOptions? jsonSerializerOptions = default, CancellationToken cancellationToken = default)
    {
        return Execute<T>(httpClient, httpRequestMessage, HttpCompletionOption.ResponseContentRead, jsonSerializerOptions, cancellationToken);
    }

#if NET7_0_OR_GREATER
    /// <summary>
    /// Executes an HTTP request and returns the result as a deserialized object of the specified type using default <see cref="HttpCompletionOption"/>.
    /// </summary>
    /// <typeparam name="T">The type of object to deserialize the result into.</typeparam>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="httpRequestMessage">The <see cref="HttpRequestMessage"/> representing the request.</param>
    /// <param name="jsonTypeInfo">The <see cref="JsonTypeInfo"/> metadata to use when deserializing the result.</param>
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
    /// There is no compatible <see cref="JsonConverter"/>
    /// for <typeparamref name="T"/> or its serializable members.
    /// </exception>
    public static Task<HttpResult<T>> Execute<T>(this HttpClient httpClient, HttpRequestMessage httpRequestMessage, JsonTypeInfo<T> jsonTypeInfo, CancellationToken cancellationToken = default)
    {
        return Execute(httpClient, httpRequestMessage, HttpCompletionOption.ResponseContentRead, jsonTypeInfo, cancellationToken);
    }
#endif

    /// <summary>
    /// Executes an HTTP request and returns the result as an <see cref="HttpResult"/> object using the specified <see cref="HttpMethod"/> and URI.
    /// </summary>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="httpMethod">The <see cref="HttpMethod"/> representing the request.</param>
    /// <param name="uri">The URI of the request.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use when deserializing the result.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="HttpResult"/> object representing the result to the request.</returns>
    public static Task<HttpResult> Execute(this HttpClient httpClient, HttpMethod httpMethod, Uri uri, JsonSerializerOptions? jsonSerializerOptions = default, CancellationToken cancellationToken = default)
    {
        return Execute(httpClient, new(httpMethod, uri), jsonSerializerOptions, cancellationToken);
    }

    /// <summary>
    /// Executes an HTTP request and returns the result as an <see cref="HttpResult"/> object using the specified <see cref="HttpMethod"/> and URI.
    /// </summary>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="httpMethod">The <see cref="HttpMethod"/> representing the request.</param>
    /// <param name="uri">The URI of the request.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use when deserializing the result.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="HttpResult"/> object representing the result to the request.</returns>
    public static Task<HttpResult> Execute(this HttpClient httpClient, HttpMethod httpMethod, string uri, JsonSerializerOptions? jsonSerializerOptions = default, CancellationToken cancellationToken = default)
    {
        var finalUri = Uri.TryCreate(uri, UriKind.Absolute, out var absoluteUri)
            ? absoluteUri
            : (httpClient.BaseAddress != null
                ? new Uri(httpClient.BaseAddress, uri)
                : throw new InvalidOperationException("HttpClient.BaseAddress is null, and the URI is not absolute."));

        return Execute(httpClient, httpMethod, finalUri, jsonSerializerOptions, cancellationToken);
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
    /// There is no compatible <see cref="JsonConverter"/>
    /// for <typeparamref name="T"/> or its serializable members.
    /// </exception>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode(RequiresDynamicCode)]
#endif
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    public static Task<HttpResult<T>> Execute<T>(this HttpClient httpClient, HttpMethod httpMethod, Uri uri, JsonSerializerOptions? jsonSerializerOptions = default, CancellationToken cancellationToken = default)
    {
        return Execute<T>(httpClient, new(httpMethod, uri), jsonSerializerOptions, cancellationToken);
    }

#if NET7_0_OR_GREATER
    /// <summary>
    /// Executes an HTTP request and returns the result as a deserialized object of the specified type using the specified <see cref="HttpMethod"/> and URI.
    /// </summary>
    /// <typeparam name="T">The type of object to deserialize the result into.</typeparam>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="httpMethod">The <see cref="HttpMethod"/> representing the request.</param>
    /// <param name="uri">The URI of the request.</param>
    /// <param name="jsonTypeInfo">The <see cref="JsonTypeInfo"/> metadata to use when deserializing the result.</param>
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
    /// There is no compatible <see cref="JsonConverter"/>
    /// for <typeparamref name="T"/> or its serializable members.
    /// </exception>
    public static Task<HttpResult<T>> Execute<T>(this HttpClient httpClient, HttpMethod httpMethod, Uri uri, JsonTypeInfo<T> jsonTypeInfo, CancellationToken cancellationToken = default)
    {
        return Execute(httpClient, new(httpMethod, uri), jsonTypeInfo, cancellationToken);
    }
#endif

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
    /// There is no compatible <see cref="JsonConverter"/>
    /// for <typeparamref name="T"/> or its serializable members.
    /// </exception>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode(RequiresDynamicCode)]
#endif
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    public static Task<HttpResult<T>> Execute<T>(this HttpClient httpClient, HttpMethod httpMethod, string uri, JsonSerializerOptions? jsonSerializerOptions = default, CancellationToken cancellationToken = default)
    {
        var finalUri = Uri.TryCreate(uri, UriKind.Absolute, out var absoluteUri)
            ? absoluteUri
            : (httpClient.BaseAddress != null
                ? new Uri(httpClient.BaseAddress, uri)
                : throw new InvalidOperationException("HttpClient.BaseAddress is null, and the URI is not absolute."));

        return Execute<T>(httpClient, httpMethod, finalUri, jsonSerializerOptions, cancellationToken);
    }

#if NET7_0_OR_GREATER
    /// <summary>
    /// Executes an HTTP request and returns the result as a deserialized object of the specified type using the specified <see cref="HttpMethod"/> and URI.
    /// </summary>
    /// <typeparam name="T">The type of object to deserialize the result into.</typeparam>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="httpMethod">The <see cref="HttpMethod"/> representing the request.</param>
    /// <param name="uri">The URI of the request.</param>
    /// <param name="jsonTypeInfo">The <see cref="JsonTypeInfo"/> metadata to use when deserializing the result.</param>
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
    /// There is no compatible <see cref="JsonConverter"/>
    /// for <typeparamref name="T"/> or its serializable members.
    /// </exception>
    public static Task<HttpResult<T>> Execute<T>(this HttpClient httpClient, HttpMethod httpMethod, string uri, JsonTypeInfo<T> jsonTypeInfo, CancellationToken cancellationToken = default)
    {
        var finalUri = Uri.TryCreate(uri, UriKind.Absolute, out var absoluteUri)
            ? absoluteUri
            : (httpClient.BaseAddress != null
                ? new Uri(httpClient.BaseAddress, uri)
                : throw new InvalidOperationException("HttpClient.BaseAddress is null, and the URI is not absolute."));

        return Execute<T>(httpClient, httpMethod, finalUri, jsonTypeInfo, cancellationToken);
    }
#endif

    /// <summary>
    /// Executes an HTTP request with a provided <see cref="Stream"/> as the content and returns the result as an <see cref="HttpResult"/> object.
    /// </summary>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="contentStream">The <see cref="Stream"/> representing the content of the request.</param>
    /// <param name="httpMethod">The <see cref="HttpMethod"/> representing the request.</param>
    /// <param name="uri">The URI of the request.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use when deserializing the result.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="HttpResult"/> object representing the result to the request.</returns>
    public static Task<HttpResult> ExecuteWithContent(this HttpClient httpClient, Stream contentStream, HttpMethod httpMethod, Uri uri, JsonSerializerOptions? jsonSerializerOptions = default, CancellationToken cancellationToken = default)
    {
        contentStream.Seek(0, SeekOrigin.Begin);

        StreamContent streamContent = new(contentStream);
        streamContent.Headers.ContentType = new("application/json")
        {
            CharSet = Encoding.UTF8.WebName
        };
        HttpRequestMessage request = new(httpMethod, uri)
        {
            Content = streamContent
        };

        return Execute(httpClient, request, jsonSerializerOptions, cancellationToken);
    }

    /// <summary>
    /// Executes an HTTP request with a provided <see cref="Stream"/> as the content and returns the result as an <see cref="HttpResult"/> object.
    /// </summary>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="contentStream">The <see cref="Stream"/> representing the content of the request.</param>
    /// <param name="httpMethod">The <see cref="HttpMethod"/> representing the request.</param>
    /// <param name="uri">The URI of the request.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use when deserializing the result.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="HttpResult"/> object representing the result to the request.</returns>
    public static Task<HttpResult> ExecuteWithContent(this HttpClient httpClient, Stream contentStream, HttpMethod httpMethod, string uri, JsonSerializerOptions? jsonSerializerOptions = default, CancellationToken cancellationToken = default)
    {
        var finalUri = Uri.TryCreate(uri, UriKind.Absolute, out var absoluteUri)
            ? absoluteUri
            : (httpClient.BaseAddress != null
                ? new Uri(httpClient.BaseAddress, uri)
                : throw new InvalidOperationException("HttpClient.BaseAddress is null, and the URI is not absolute."));

        return ExecuteWithContent(httpClient, contentStream, httpMethod, finalUri, jsonSerializerOptions, cancellationToken);
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
    /// There is no compatible <see cref="JsonConverter"/>
    /// for <typeparamref name="T"/> or its serializable members.
    /// </exception>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode(RequiresDynamicCode)]
#endif
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    public static Task<HttpResult<T>> ExecuteWithContent<T>(this HttpClient httpClient, Stream contentStream, HttpMethod httpMethod, Uri uri, JsonSerializerOptions? jsonSerializerOptions = default, CancellationToken cancellationToken = default)
    {
        contentStream.Seek(0, SeekOrigin.Begin);

        StreamContent streamContent = new(contentStream);
        streamContent.Headers.ContentType = new("application/json")
        {
            CharSet = Encoding.UTF8.WebName
        };
        HttpRequestMessage request = new(httpMethod, uri)
        {
            Content = streamContent
        };

        return Execute<T>(httpClient, request, jsonSerializerOptions, cancellationToken);
    }

#if NET7_0_OR_GREATER
    /// <summary>
    /// Executes an HTTP request with a provided <see cref="Stream"/> as the content and returns the result as a deserialized object of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of object to deserialize the result into.</typeparam>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="contentStream">The <see cref="Stream"/> representing the content of the request.</param>
    /// <param name="httpMethod">The <see cref="HttpMethod"/> representing the request.</param>
    /// <param name="uri">The URI of the request.</param>
    /// <param name="jsonTypeInfo">The <see cref="JsonTypeInfo"/> metadata to use when deserializing the result.</param>
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
    /// There is no compatible <see cref="JsonConverter"/>
    /// for <typeparamref name="T"/> or its serializable members.
    /// </exception>
    public static Task<HttpResult<T>> ExecuteWithContent<T>(this HttpClient httpClient, Stream contentStream, HttpMethod httpMethod, Uri uri, JsonTypeInfo<T> jsonTypeInfo, CancellationToken cancellationToken = default)
    {
        contentStream.Seek(0, SeekOrigin.Begin);

        StreamContent streamContent = new(contentStream);
        streamContent.Headers.ContentType = new("application/json")
        {
            CharSet = Encoding.UTF8.WebName
        };
        HttpRequestMessage request = new(httpMethod, uri)
        {
            Content = streamContent
        };

        return Execute(httpClient, request, jsonTypeInfo, cancellationToken);
    }
#endif

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
    /// There is no compatible <see cref="JsonConverter"/>
    /// for <typeparamref name="T"/> or its serializable members.
    /// </exception>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode(RequiresDynamicCode)]
#endif
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    public static Task<HttpResult<T>> ExecuteWithContent<T>(this HttpClient httpClient, Stream contentStream, HttpMethod httpMethod, string uri, JsonSerializerOptions? jsonSerializerOptions = default, CancellationToken cancellationToken = default)
    {
        var finalUri = Uri.TryCreate(uri, UriKind.Absolute, out var absoluteUri)
            ? absoluteUri
            : (httpClient.BaseAddress != null
                ? new Uri(httpClient.BaseAddress, uri)
                : throw new InvalidOperationException("HttpClient.BaseAddress is null, and the URI is not absolute."));

        return ExecuteWithContent<T>(httpClient, contentStream, httpMethod, finalUri, jsonSerializerOptions, cancellationToken);
    }

#if NET7_0_OR_GREATER
    /// <summary>
    /// Executes an HTTP request with a provided <see cref="Stream"/> as the content and returns the result as a deserialized object of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of object to deserialize the result into.</typeparam>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="contentStream">The <see cref="Stream"/> representing the content of the request.</param>
    /// <param name="httpMethod">The <see cref="HttpMethod"/> representing the request.</param>
    /// <param name="uri">The URI of the request.</param>
    /// <param name="jsonTypeInfo">The <see cref="JsonTypeInfo"/> metadata to use when deserializing the result.</param>
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
    /// There is no compatible <see cref="JsonConverter"/>
    /// for <typeparamref name="T"/> or its serializable members.
    /// </exception>
    public static Task<HttpResult<T>> ExecuteWithContent<T>(this HttpClient httpClient, Stream contentStream, HttpMethod httpMethod, string uri, JsonTypeInfo<T> jsonTypeInfo, CancellationToken cancellationToken = default)
    {
        var finalUri = Uri.TryCreate(uri, UriKind.Absolute, out var absoluteUri)
            ? absoluteUri
            : (httpClient.BaseAddress != null
                ? new Uri(httpClient.BaseAddress, uri)
                : throw new InvalidOperationException("HttpClient.BaseAddress is null, and the URI is not absolute."));

        return ExecuteWithContent<T>(httpClient, contentStream, httpMethod, finalUri, jsonTypeInfo, cancellationToken);
    }
#endif

    /// <summary>
    /// Executes an HTTP request with a provided string as the content and returns the result as an <see cref="HttpResult"/> object.
    /// </summary>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="content">The string representing the content of the request.</param>
    /// <param name="httpMethod">The <see cref="HttpMethod"/> representing the request.</param>
    /// <param name="uri">The URI of the request.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use when deserializing the result.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="HttpResult"/> object representing the result to the request.</returns>
    public static Task<HttpResult> ExecuteWithContent(this HttpClient httpClient, string content, HttpMethod httpMethod, Uri uri, JsonSerializerOptions? jsonSerializerOptions = default, CancellationToken cancellationToken = default)
    {
        HttpRequestMessage request = new(httpMethod, uri)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };

        return Execute(httpClient, request, jsonSerializerOptions, cancellationToken);
    }

    /// <summary>
    /// Executes an HTTP request with a provided string as the content and returns the result as an <see cref="HttpResult"/> object.
    /// </summary>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="content">The string representing the content of the request.</param>
    /// <param name="httpMethod">The <see cref="HttpMethod"/> representing the request.</param>
    /// <param name="uri">The URI of the request.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use when deserializing the result.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="HttpResult"/> object representing the result to the request.</returns>
    public static Task<HttpResult> ExecuteWithContent(this HttpClient httpClient, string content, HttpMethod httpMethod, string uri, JsonSerializerOptions? jsonSerializerOptions = default, CancellationToken cancellationToken = default)
    {
        var finalUri = Uri.TryCreate(uri, UriKind.Absolute, out var absoluteUri)
            ? absoluteUri
            : (httpClient.BaseAddress != null
                ? new Uri(httpClient.BaseAddress, uri)
                : throw new InvalidOperationException("HttpClient.BaseAddress is null, and the URI is not absolute."));

        return ExecuteWithContent(httpClient, content, httpMethod, finalUri, jsonSerializerOptions, cancellationToken);
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
    /// There is no compatible <see cref="JsonConverter"/>
    /// for <typeparamref name="T"/> or its serializable members.
    /// </exception>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode(RequiresDynamicCode)]
#endif
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    public static Task<HttpResult<T>> ExecuteWithContent<T>(this HttpClient httpClient, string content, HttpMethod httpMethod, Uri uri, JsonSerializerOptions? jsonSerializerOptions = default, CancellationToken cancellationToken = default)
    {
        HttpRequestMessage request = new(httpMethod, uri)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };

        return Execute<T>(httpClient, request, jsonSerializerOptions, cancellationToken);
    }

#if NET7_0_OR_GREATER
    /// <summary>
    /// Executes an HTTP request with a provided string as the content and returns the result as a deserialized object of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of object to deserialize the result into.</typeparam>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="content">The string representing the content of the request.</param>
    /// <param name="httpMethod">The <see cref="HttpMethod"/> representing the request.</param>
    /// <param name="uri">The URI of the request.</param>
    /// <param name="jsonTypeInfo">The <see cref="JsonTypeInfo"/> metadata to use when deserializing the result.</param>
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
    /// There is no compatible <see cref="JsonConverter"/>
    /// for <typeparamref name="T"/> or its serializable members.
    /// </exception>
    public static Task<HttpResult<T>> ExecuteWithContent<T>(this HttpClient httpClient, string content, HttpMethod httpMethod, Uri uri, JsonTypeInfo<T> jsonTypeInfo, CancellationToken cancellationToken = default)
    {
        HttpRequestMessage request = new(httpMethod, uri)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };

        return Execute<T>(httpClient, request, jsonTypeInfo, cancellationToken);
    }
#endif

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
    /// There is no compatible <see cref="JsonConverter"/>
    /// for <typeparamref name="T"/> or its serializable members.
    /// </exception>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode(RequiresDynamicCode)]
#endif
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    public static Task<HttpResult<T>> ExecuteWithContent<T>(this HttpClient httpClient, string content, HttpMethod httpMethod, string uri, JsonSerializerOptions? jsonSerializerOptions = default, CancellationToken cancellationToken = default)
    {
        var finalUri = Uri.TryCreate(uri, UriKind.Absolute, out var absoluteUri)
            ? absoluteUri
            : (httpClient.BaseAddress != null
                ? new Uri(httpClient.BaseAddress, uri)
                : throw new InvalidOperationException("HttpClient.BaseAddress is null, and the URI is not absolute."));

        return ExecuteWithContent<T>(httpClient, content, httpMethod, finalUri, jsonSerializerOptions, cancellationToken);
    }

#if NET7_0_OR_GREATER
    /// <summary>
    /// Executes an HTTP request with a provided string as the content and returns the result as a deserialized object of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of object to deserialize the result into.</typeparam>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="content">The string representing the content of the request.</param>
    /// <param name="httpMethod">The <see cref="HttpMethod"/> representing the request.</param>
    /// <param name="uri">The URI of the request.</param>
    /// <param name="jsonTypeInfo">The <see cref="JsonTypeInfo"/> metadata to use when deserializing the result.</param>
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
    /// There is no compatible <see cref="JsonConverter"/>
    /// for <typeparamref name="T"/> or its serializable members.
    /// </exception>
    public static Task<HttpResult<T>> ExecuteWithContent<T>(this HttpClient httpClient, string content, HttpMethod httpMethod, string uri, JsonTypeInfo<T> jsonTypeInfo, CancellationToken cancellationToken = default)
    {
        var finalUri = Uri.TryCreate(uri, UriKind.Absolute, out var absoluteUri)
            ? absoluteUri
            : (httpClient.BaseAddress != null
                ? new Uri(httpClient.BaseAddress, uri)
                : throw new InvalidOperationException("HttpClient.BaseAddress is null, and the URI is not absolute."));

        return ExecuteWithContent<T>(httpClient, content, httpMethod, finalUri, jsonTypeInfo, cancellationToken);
    }
#endif

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
#if NET7_0_OR_GREATER
    [RequiresDynamicCode(RequiresDynamicCode)]
#endif
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    public static Task<HttpResult> ExecuteWithContent<TContent>(this HttpClient httpClient, TContent content, HttpMethod httpMethod, Uri uri, JsonSerializerOptions? jsonSerializerOptions = default, CancellationToken cancellationToken = default)
    {
        HttpRequestMessage request = new(httpMethod, uri)
        {
            Content = new StringContent(JsonSerializer.Serialize(content, jsonSerializerOptions), Encoding.UTF8, "application/json")
        };

        return Execute(httpClient, request, jsonSerializerOptions, cancellationToken);
    }

#if NET7_0_OR_GREATER
    /// <summary>
    /// Executes an HTTP request with a provided <typeparamref name="TContent"/> as the content and returns the result as an <see cref="HttpResult"/> object.
    /// </summary>
    /// <typeparam name="TContent">The type of content to serialize.</typeparam>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="content">The string representing the content of the request.</param>
    /// <param name="httpMethod">The <see cref="HttpMethod"/> representing the request.</param>
    /// <param name="uri">The URI of the request.</param>
    /// <param name="jsonTypeInfo">A <see cref="JsonTypeInfo"/> metadata that can be used to serialize <paramref name="content"/>.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="HttpResult"/> object representing the result to the request.</returns>
    public static Task<HttpResult> ExecuteWithContent<TContent>(this HttpClient httpClient, TContent content, HttpMethod httpMethod, Uri uri, JsonTypeInfo<TContent> jsonTypeInfo, CancellationToken cancellationToken = default)
    {
        HttpRequestMessage request = new(httpMethod, uri)
        {
            Content = new StringContent(JsonSerializer.Serialize(content, jsonTypeInfo), Encoding.UTF8, "application/json")
        };

        return Execute(httpClient, request, jsonTypeInfo.Options, cancellationToken);
    }
#endif

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
#if NET7_0_OR_GREATER
    [RequiresDynamicCode(RequiresDynamicCode)]
#endif
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    public static Task<HttpResult> ExecuteWithContent<TContent>(this HttpClient httpClient, TContent content, HttpMethod httpMethod, string uri, JsonSerializerOptions? jsonSerializerOptions = default, CancellationToken cancellationToken = default)
    {
        var finalUri = Uri.TryCreate(uri, UriKind.Absolute, out var absoluteUri)
            ? absoluteUri
            : (httpClient.BaseAddress != null
                ? new Uri(httpClient.BaseAddress, uri)
                : throw new InvalidOperationException("HttpClient.BaseAddress is null, and the URI is not absolute."));

        return ExecuteWithContent(httpClient, JsonSerializer.Serialize(content, jsonSerializerOptions), httpMethod, finalUri, jsonSerializerOptions, cancellationToken);
    }

#if NET7_0_OR_GREATER
    /// <summary>
    /// Executes an HTTP request with a provided string as the content and returns the result as an <see cref="HttpResult"/> object.
    /// </summary>
    /// <typeparam name="TContent">The type of content to serialize.</typeparam>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="content">The string representing the content of the request.</param>
    /// <param name="httpMethod">The <see cref="HttpMethod"/> representing the request.</param>
    /// <param name="uri">The URI of the request.</param>
    /// <param name="jsonTypeInfo">A <see cref="JsonTypeInfo"/> metadata that can be used to serialize <paramref name="content"/>.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="HttpResult"/> object representing the result to the request.</returns>
    public static Task<HttpResult> ExecuteWithContent<TContent>(this HttpClient httpClient, TContent content, HttpMethod httpMethod, string uri, JsonTypeInfo<TContent> jsonTypeInfo, CancellationToken cancellationToken = default)
    {
        var finalUri = Uri.TryCreate(uri, UriKind.Absolute, out var absoluteUri)
            ? absoluteUri
            : (httpClient.BaseAddress != null
                ? new Uri(httpClient.BaseAddress, uri)
                : throw new InvalidOperationException("HttpClient.BaseAddress is null, and the URI is not absolute."));

        return ExecuteWithContent(httpClient, JsonSerializer.Serialize(content, jsonTypeInfo), httpMethod, finalUri, jsonTypeInfo.Options, cancellationToken);
    }
#endif

    /// <summary>
    /// Executes an HTTP request with a provided <typeparamref name="TContent"/> as the content and returns the result as a deserialized object of the specified type.
    /// </summary>
    /// <typeparam name="TContent">The type of content to serialize.</typeparam>
    /// <typeparam name="TResponse">The type of object to deserialize the result into.</typeparam>
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
    /// <typeparamref name="TResponse" /> is not compatible with the JSON.
    ///
    /// -or-
    ///
    /// There is remaining data in the string beyond a single JSON value.</exception>
    /// <exception cref="NotSupportedException">
    /// There is no compatible <see cref="JsonConverter"/>
    /// for <typeparamref name="TResponse"/> or its serializable members.
    /// </exception>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode(RequiresDynamicCode)]
#endif
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    public static Task<HttpResult<TResponse>> ExecuteWithContent<TContent, TResponse>(this HttpClient httpClient, TContent content, HttpMethod httpMethod, Uri uri, JsonSerializerOptions? jsonSerializerOptions = default, CancellationToken cancellationToken = default)
    {
        HttpRequestMessage request = new(httpMethod, uri)
        {
            Content = new StringContent(JsonSerializer.Serialize(content, jsonSerializerOptions), Encoding.UTF8, "application/json")
        };

        return Execute<TResponse>(httpClient, request, jsonSerializerOptions, cancellationToken);
    }

#if NET7_0_OR_GREATER
    /// <summary>
    /// Executes an HTTP request with a provided <typeparamref name="TContent"/> as the content and returns the result as a deserialized object of the specified type.
    /// </summary>
    /// <typeparam name="TContent">The type of content to serialize.</typeparam>
    /// <typeparam name="TResponse">The type of object to deserialize the result into.</typeparam>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="content">The string representing the content of the request.</param>
    /// <param name="httpMethod">The <see cref="HttpMethod"/> representing the request.</param>
    /// <param name="uri">The URI of the request.</param>
    /// <param name="contentJsonTypeInfo">The <see cref="JsonTypeInfo"/> to use when deserializing the content.</param>
    /// <param name="responseJsonTypeInfo">The <see cref="JsonTypeInfo"/> to use when deserializing the result.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="HttpResult{T}"/> object representing the result to the request and the deserialized object.</returns>
    /// <exception cref="JsonException">
    /// The JSON is invalid.
    ///
    /// -or-
    ///
    /// <typeparamref name="TResponse" /> is not compatible with the JSON.
    ///
    /// -or-
    ///
    /// There is remaining data in the string beyond a single JSON value.</exception>
    /// <exception cref="NotSupportedException">
    /// There is no compatible <see cref="JsonConverter"/>
    /// for <typeparamref name="TResponse"/> or its serializable members.
    /// </exception>
    public static Task<HttpResult<TResponse>> ExecuteWithContent<TContent, TResponse>(this HttpClient httpClient, TContent content, HttpMethod httpMethod, Uri uri, JsonTypeInfo<TContent> contentJsonTypeInfo, JsonTypeInfo<TResponse> responseJsonTypeInfo, CancellationToken cancellationToken = default)
    {
        HttpRequestMessage request = new(httpMethod, uri)
        {
            Content = new StringContent(JsonSerializer.Serialize(content, contentJsonTypeInfo), Encoding.UTF8, "application/json")
        };

        return Execute(httpClient, request, responseJsonTypeInfo, cancellationToken);
    }
#endif

    /// <summary>
    /// Executes an HTTP request with a provided <typeparamref name="TContent"/> as the content and returns the result as a deserialized object of the specified type.
    /// </summary>
    /// <typeparam name="TContent">The type of content to serialize.</typeparam>
    /// <typeparam name="TResponse">The type of object to deserialize the result into.</typeparam>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="content">The string representing the content of the request.</param>
    /// <param name="httpMethod">The <see cref="HttpMethod"/> representing the request.</param>
    /// <param name="uri">The URI of the request.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use when deserializing the result.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="HttpResult{TResponse}"/> object representing the result to the request and the deserialized object.</returns>
    /// <exception cref="JsonException">
    /// The JSON is invalid.
    ///
    /// -or-
    ///
    /// <typeparamref name="TResponse" /> is not compatible with the JSON.
    ///
    /// -or-
    ///
    /// There is remaining data in the string beyond a single JSON value.</exception>
    /// <exception cref="NotSupportedException">
    /// There is no compatible <see cref="JsonConverter"/>
    /// for <typeparamref name="TResponse"/> or its serializable members.
    /// </exception>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode(RequiresDynamicCode)]
#endif
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    public static Task<HttpResult<TResponse>> ExecuteWithContent<TContent, TResponse>(this HttpClient httpClient, TContent content, HttpMethod httpMethod, string uri, JsonSerializerOptions? jsonSerializerOptions = default, CancellationToken cancellationToken = default)
    {
        var finalUri = Uri.TryCreate(uri, UriKind.Absolute, out var absoluteUri)
            ? absoluteUri
            : (httpClient.BaseAddress != null
                ? new Uri(httpClient.BaseAddress, uri)
                : throw new InvalidOperationException("HttpClient.BaseAddress is null, and the URI is not absolute."));

        return ExecuteWithContent<TContent, TResponse>(httpClient, content, httpMethod, finalUri, jsonSerializerOptions, cancellationToken);
    }

#if NET7_0_OR_GREATER
    /// <summary>
    /// Executes an HTTP request with a provided <typeparamref name="TContent"/> as the content and returns the result as a deserialized object of the specified type.
    /// </summary>
    /// <typeparam name="TContent">The type of content to serialize.</typeparam>
    /// <typeparam name="TResponse">The type of object to deserialize the result into.</typeparam>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use for the request.</param>
    /// <param name="content">The string representing the content of the request.</param>
    /// <param name="httpMethod">The <see cref="HttpMethod"/> representing the request.</param>
    /// <param name="uri">The URI of the request.</param>
    /// <param name="contentJsonTypeInfo">The <see cref="JsonTypeInfo"/> metadata to use when deserializing the content.</param>
    /// <param name="responseJsonTypeInfo">The <see cref="JsonTypeInfo"/> metadata to use when deserializing the result.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>An <see cref="HttpResult{TResponse}"/> object representing the result to the request and the deserialized object.</returns>
    /// <exception cref="JsonException">
    /// The JSON is invalid.
    ///
    /// -or-
    ///
    /// <typeparamref name="TResponse" /> is not compatible with the JSON.
    ///
    /// -or-
    ///
    /// There is remaining data in the string beyond a single JSON value.</exception>
    /// <exception cref="NotSupportedException">
    /// There is no compatible <see cref="JsonConverter"/>
    /// for <typeparamref name="TResponse"/> or its serializable members.
    /// </exception>
    public static Task<HttpResult<TResponse>> ExecuteWithContent<TContent, TResponse>(this HttpClient httpClient, TContent content, HttpMethod httpMethod, string uri, JsonTypeInfo<TContent> contentJsonTypeInfo, JsonTypeInfo<TResponse> responseJsonTypeInfo, CancellationToken cancellationToken = default)
    {
        var finalUri = Uri.TryCreate(uri, UriKind.Absolute, out var absoluteUri)
            ? absoluteUri
            : (httpClient.BaseAddress != null
                ? new Uri(httpClient.BaseAddress, uri)
                : throw new InvalidOperationException("HttpClient.BaseAddress is null, and the URI is not absolute."));

        return ExecuteWithContent(httpClient, content, httpMethod, finalUri, contentJsonTypeInfo, responseJsonTypeInfo, cancellationToken);
    }
#endif
}
