using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace RestfulHelpers.Common;

/// <summary>
/// The base response for all HTTP requests.
/// </summary>
public class HttpTransaction
{
    /// <summary>
    /// Gets the request URL of the response.
    /// </summary>
    public string? RequestUrl { get; }

    /// <summary>
    /// Gets the <see cref="HttpRequestMessage"/> of the request.
    /// </summary>
    public HttpRequestMessage? RequestMessage { get; }

    /// <summary>
    /// Gets the <see cref="HttpResponseMessage"/> of the request.
    /// </summary>
    public HttpResponseMessage? ResponseMessage { get; }

    /// <summary>
    /// Gets the <see cref="HttpStatusCode"/> of the request.
    /// </summary>
    public HttpStatusCode? StatusCode { get; }

    internal HttpTransaction(HttpRequestMessage? request, HttpResponseMessage? response, HttpStatusCode? httpStatusCode)
    {
        RequestUrl = request?.RequestUri?.ToString();
        RequestMessage = request;
        ResponseMessage = response;
        StatusCode = httpStatusCode;
    }

    /// <summary>
    /// Gets the <see cref="HttpRequestMessage.Content"/> as string.
    /// </summary>
    /// <returns>
    /// The <see cref="Task"/> that represents the string content.
    /// </returns>
    public async Task<string?> GetRequestContentAsString()
    {
        if (RequestMessage?.Content == null)
        {
            return null;
        }

        return await RequestMessage.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Gets the <see cref="HttpResponseMessage.Content"/> as string.
    /// </summary>
    /// <returns>
    /// The <see cref="Task"/> that represents the string content.
    /// </returns>
    public async Task<string?> GetResponseContentAsString()
    {
        if (ResponseMessage?.Content == null)
        {
            return null;
        }

        return await ResponseMessage.Content.ReadAsStringAsync();
    }
}
