using SharpSql.Attributes;
using System;

namespace SharpSql.Exceptions;

[Serializable]
public class PrimaryKeyAttributeNotImplementedException : Exception
{
    public PrimaryKeyAttributeNotImplementedException(Type type)
        : base($"{nameof(SharpSqlPrimaryKeyAttribute)} is not implemented in entity of type: { type.Name }.")
    { }
}