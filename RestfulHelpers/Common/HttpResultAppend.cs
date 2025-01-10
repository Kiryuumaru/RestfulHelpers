using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using TransactionHelpers;

namespace RestfulHelpers.Common;

internal class HttpResultAppend : ResultAppend
{
    public HttpStatusCode StatusCode { get; set; }

    public IReadOnlyDictionary<string, string[]>? ResponseHeaders { get; set; }

    public bool ShouldAppendStatusCode { get; set; }

    public bool ShouldAppendStatusCodeOrError { get; set; }

    public bool ShouldAppendHeaders { get; set; }

    public bool ShouldReplaceHeaders { get; set; }

    public bool ShouldAppendHeaderValues { get; set; }

    public bool ShouldReplaceHeaderValues { get; set; }
}
