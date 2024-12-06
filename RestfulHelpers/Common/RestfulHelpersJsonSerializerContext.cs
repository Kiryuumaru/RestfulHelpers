using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TransactionHelpers;
using TransactionHelpers.Interface;
using RestfulHelpers.Interface;

namespace RestfulHelpers.Common;

#if NET7_0_OR_GREATER
[JsonSerializable(typeof(Result))]
[JsonSerializable(typeof(Error))]
[JsonSerializable(typeof(HttpResult))]
[JsonSerializable(typeof(HttpError))]
[JsonSerializable(typeof(List<Error>))]
[JsonSerializable(typeof(IResult))]
[JsonSerializable(typeof(IHttpResult))]
internal partial class RestfulHelpersJsonSerializerContext : JsonSerializerContext
{
    internal static RestfulHelpersJsonSerializerContext WebDefault { get; } = new(new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    });
}
#endif