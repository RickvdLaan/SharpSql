using System;

namespace SharpSql.Exceptions;

[Serializable]
public class PropertyNotNullableException : Exception
{
    public PropertyNotNullableException()
    { }

    public PropertyNotNullableException(string message)
        : base(message)
    { }

    public PropertyNotNullableException(string message, Exception innerException)
        : base(message, innerException)
    { }
}