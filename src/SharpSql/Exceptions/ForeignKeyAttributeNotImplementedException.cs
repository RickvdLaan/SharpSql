using SharpSql.Attributes;
using System;
using System.Reflection;

namespace SharpSql.Exceptions;

[Serializable]
public class ForeignKeyAttributeNotImplementedException : Exception
{
    public ForeignKeyAttributeNotImplementedException(PropertyInfo propertyInfo, Type type)
        : base($"{nameof(SharpSqlForeignKeyAttribute)} is not implemented on property { propertyInfo.Name } in { type.Name }.")
    { }
}