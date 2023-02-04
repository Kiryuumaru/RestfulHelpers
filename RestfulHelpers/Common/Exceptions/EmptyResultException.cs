using System;
using System.Collections.Generic;
using System.Text;

namespace RestfulHelpers.Common.Exceptions;

/// <summary>
/// Exception for responses with no results and no error.
/// </summary>
public class EmptyResultException : Exception
{
    internal EmptyResultException()
        : base("The response has no result.")
    {
        
    }
}
