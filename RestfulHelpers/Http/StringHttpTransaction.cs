using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace RestfulHelpers;

public class StringHttpTransaction
{
    public string RequestUrl { get; }

    public string? RequestMessage { get; }

    public string? ResponseMessage { get; }

    public HttpStatusCode StatusCode { get; }

    internal StringHttpTransaction(string url, string? request, string? response, HttpStatusCode httpStatusCode)
    {
        RequestUrl = url;
        RequestMessage = request;
        ResponseMessage = response;
        StatusCode = httpStatusCode;
    }
}
