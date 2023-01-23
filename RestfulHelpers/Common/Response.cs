using RestfulHelpers.Interface;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace RestfulHelpers.Common;

/// <summary>
/// The responses for all API request.
/// </summary>
public class Response : IResponse
{
    /// <inheritdoc/>
    public virtual Exception? Error { get; protected set; }

    /// <inheritdoc/>
    [MemberNotNullWhen(false, nameof(Error))]
    public virtual bool IsSuccess { get => Error == null; }

    /// <inheritdoc/>
    [MemberNotNullWhen(true, nameof(Error))]
    public virtual bool IsError { get => Error != null; }

    /// <inheritdoc/>
    public virtual void ThrowIfError()
    {
        if (Error != null)
        {
            throw Error;
        }
    }

    /// <summary>
    /// Append <see cref="IResponse"/> to the response.
    /// </summary>
    /// <param name="responses">The responses <see cref="IResponse"/> to append.</param>
    public virtual void Append(params IResponse[] responses)
    {
        if (responses.LastOrDefault() is IResponse lastResponse)
        {
            Error = lastResponse.Error;
        }
    }

    /// <summary>
    /// Append an exception to the response.
    /// </summary>
    /// <param name="error">The exception to append.</param>
    public virtual void Append(Exception? error)
    {
        Error = error;
    }
}

/// <summary>
/// The responses for all API request.
/// </summary>
/// <typeparam name="TResult">
/// The type of the operation response.
/// </typeparam>
public class Response<TResult> : IResponse
{
    private Exception? error;

    /// <summary>
    /// Gets the <typeparamref name="TResult"/> of the operation.
    /// </summary>
    public virtual TResult? Result { get; protected set; }

    /// <inheritdoc/>
    public Exception? Error
    {
        get => error == null && Result == null ? new NullReferenceException($"Response has no \"{nameof(Result)}\".") : error;
        set => error = value;
    }

    /// <inheritdoc/>
    [MemberNotNullWhen(false, nameof(Error))]
    [MemberNotNullWhen(true, nameof(Result))]
    public virtual bool IsSuccess { get => error == null && Result != null; }

    /// <inheritdoc/>
    [MemberNotNullWhen(true, nameof(Error))]
    [MemberNotNullWhen(false, nameof(Result))]
    public virtual bool IsError { get => error != null || Result == null; }

    /// <inheritdoc/>
    [MemberNotNull(nameof(Result))]
    public virtual void ThrowIfError()
    {
        if (IsError)
        {
            throw Error;
        }
    }

    /// <summary>
    /// Append <see cref="IResponse"/> to the response.
    /// </summary>
    /// <param name="responses">The responses <see cref="IResponse"/> to append.</param>
    public virtual void Append(params IResponse[] responses)
    {
        if (responses.LastOrDefault() is IResponse lastResponse)
        {
            Error = lastResponse.Error;
            if (lastResponse is Response<TResult> lastTypedResponse)
            {
                Result = lastTypedResponse.Result;
            }
        }
    }

    /// <summary>
    /// Append an exception to the response.
    /// </summary>
    /// <param name="error">The exception to append.</param>
    public virtual void Append(Exception? error)
    {
        Error = error;
    }

    /// <summary>
    /// Append <typeparamref name="TResult"/> to the response.
    /// </summary>
    /// <param name="result">The result to append.</param>
    public virtual void Append(TResult? result)
    {
        Result = result;
    }
}