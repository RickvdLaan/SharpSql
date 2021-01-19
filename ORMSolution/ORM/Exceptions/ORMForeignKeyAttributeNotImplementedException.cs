using System;
using System.Reflection;

namespace ORM.Exceptions
{
    [Serializable]
    public class ORMForeignKeyAttributeNotImplementedException : Exception
    {
        public ORMForeignKeyAttributeNotImplementedException(PropertyInfo propertyInfo, Type type)
            : base($"ORMForeignKeyAttribute is not implemented on property { propertyInfo.Name } in { type.Name }.")
        { }
    }
}
