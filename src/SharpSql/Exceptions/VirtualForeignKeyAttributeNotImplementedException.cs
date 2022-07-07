using System;
using System.Reflection;

namespace SharpSql.Exceptions;

[Serializable]
public class VirtualForeignKeyAttributeNotImplementedException : Exception
{
    public VirtualForeignKeyAttributeNotImplementedException(PropertyInfo propertyInfo, Type type)
        : base($"{nameof(VirtualForeignKeyAttributeNotImplementedException)} is not implemented on property { propertyInfo.Name } in { type.Name }.")
    { }
}