using System.Diagnostics.CodeAnalysis;
using System;
using System.Collections.Generic;
using RestfulHelpers.Common;
using TransactionHelpers.Interface;
using TransactionHelpers;
using System.Net;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace RestfulHelpers.Interface;

#if NET7_0_OR_GREATER
/// <summary>
/// The interface for all HTTP responses.
/// </summary>
public interface IHttpResultResponse : IActionResult, Microsoft.AspNetCore.Http.IResult, Microsoft.AspNetCore.Http.IStatusCodeHttpResult
{
}

/// <summary>
/// The interface for all HTTP responses.
/// </summary>
public interface IHttpResultResponse<TValue> : IActionResult, Microsoft.AspNetCore.Http.IResult, Microsoft.AspNetCore.Http.IStatusCodeHttpResult, Microsoft.AspNetCore.Http.IValueHttpResult<HttpResult<TValue>>
{
}
#endif