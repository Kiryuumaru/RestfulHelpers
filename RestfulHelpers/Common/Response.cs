using System;
using System.Diagnostics.CodeAnalysis;

namespace RestfulHelpers;

public class Response
{
    public virtual Exception? Error { get; protected set; }

    [MemberNotNullWhen(false, nameof(Error))]
    public virtual bool IsSuccess { get => Error == null; }

    [MemberNotNullWhen(true, nameof(Error))]
    public virtual bool IsError { get => Error != null; }

    public Response()
    {

    }

    public virtual void ThrowIfError()
    {
        if (Error != null)
        {
            throw Error;
        }
    }

    public virtual void Append(params Response[] responses)
    {
        if (responses.LastOrDefault() is Response lastResponse)
        {
            Error = lastResponse.Error;
        }
    }

    public virtual void Append(Exception? error)
    {
        Error = error;
    }
}

public class Response<TResult> : Response
{
    public virtual TResult? Result { get; protected set; }

    public override Exception? Error { get; protected set; }

    [MemberNotNullWhen(false, nameof(Error))]
    [MemberNotNullWhen(true, nameof(Result))]
    public override bool IsSuccess { get => Error == null && Result != null; }

    [MemberNotNullWhen(true, nameof(Error))]
    [MemberNotNullWhen(false, nameof(Result))]
    public override bool IsError { get => !IsSuccess; }

    public Response()
    {

    }

    [MemberNotNull(nameof(Result))]
    public override void ThrowIfError()
    {
        if (Error != null)
        {
            throw Error;
        }
        else if (Result == null)
        {
            throw new NullReferenceException($"Response has no \"{nameof(Result)}\".");
        }
    }

    public override void Append(params Response[] responses)
    {
        if (responses.LastOrDefault() is Response lastResponse)
        {
            Error = lastResponse.Error;
            if (lastResponse is Response<TResult> lastTypedResponse)
            {
                Result = lastTypedResponse.Result;
            }
        }
    }

    public virtual void Append(TResult? result)
    {
        Result = result;
    }
}
