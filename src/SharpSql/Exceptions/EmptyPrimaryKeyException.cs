using System;

namespace SharpSql.Exceptions;

[Serializable]
public class EmptyPrimaryKeyException : Exception
{
    public EmptyPrimaryKeyException()
    { }

    public EmptyPrimaryKeyException(string message)
        : base(message)
    { }

    public EmptyPrimaryKeyException(string message, Exception innerException)
        : base(message, innerException)
    { }
}