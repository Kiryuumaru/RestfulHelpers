using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using TransactionHelpers;

namespace RestfulHelpers.Common;

[JsonSerializable(typeof(Result))]
[JsonSerializable(typeof(Error))]
[JsonSerializable(typeof(HttpResult))]
[JsonSerializable(typeof(HttpError))]
[JsonSerializable(typeof(List<Error>))]
internal partial class RestfulHelpersJsonSerializerContext : JsonSerializerContext
{
}
