using System.Net;
using System.Net.Http;

namespace RestfulHelpers.Common;

/// <summary>
/// The base response for all HTTP requests.
/// </summary>
public class StringHttpTransaction
{
    /// <summary>
    /// Gets the request URL of the response.
    /// </summary>
    public string? RequestUrl { get; }

    /// <summary>
    /// Gets the <see cref="HttpRequestMessage"/> of the request.
    /// </summary>
    public string? RequestMessage { get; }

    /// <summary>
    /// Gets the <see cref="HttpResponseMessage"/> of the request.
    /// </summary>
    public string? ResponseMessage { get; }

    /// <summary>
    /// Gets the <see cref="HttpStatusCode"/> of the request.
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    internal StringHttpTransaction(string? url, string? request, string? response, HttpStatusCode httpStatusCode)
    {
        RequestUrl = url;
        RequestMessage = request;
        ResponseMessage = response;
        StatusCode = httpStatusCode;
    }
}