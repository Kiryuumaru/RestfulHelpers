using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace RestfulHelpers;

public class HttpTransaction
{
    public string RequestUrl { get; }

    public HttpRequestMessage RequestMessage { get; }

    public HttpResponseMessage? ResponseMessage { get; }

    public HttpStatusCode StatusCode { get; }

    internal HttpTransaction(HttpRequestMessage request, HttpResponseMessage? response, HttpStatusCode httpStatusCode)
    {
        RequestUrl = request.RequestUri?.ToString()!;
        RequestMessage = request;
        ResponseMessage = response;
        StatusCode = httpStatusCode;
    }

    public async Task<string?> GetRequestContentAsString()
    {
        if (RequestMessage?.Content == null)
        {
            return null;
        }

        return await RequestMessage.Content.ReadAsStringAsync();
    }

    public async Task<string?> GetResponseContentAsString()
    {
        if (ResponseMessage?.Content == null)
        {
            return null;
        }

        return await ResponseMessage.Content.ReadAsStringAsync();
    }
}
