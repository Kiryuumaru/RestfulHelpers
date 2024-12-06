using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace RestfulHelpers.Common.Internals;

#if NET6_0_OR_GREATER
internal sealed class ProducesResponseTypeMetadata(int statusCode, Type type, IEnumerable<string> contentTypes) : Microsoft.AspNetCore.Http.Metadata.IProducesResponseTypeMetadata
{
    public Type? Type { get; } = type;

    public int StatusCode { get; } = statusCode;

    public IEnumerable<string> ContentTypes { get; } = contentTypes;
}
#endif