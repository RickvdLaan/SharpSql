using System;
using System.Reflection;

namespace SharpSql.Exceptions
{
    [Serializable]
    public class InvalidJoinException : Exception
    {
        public InvalidJoinException(string customMessage)
            : base(customMessage)
        { }

        public InvalidJoinException(PropertyInfo propertyInfo)
           : base($"Cannot join field [{ propertyInfo.Name }] of type [{propertyInfo.PropertyType}], since it's not of type [{ typeof(SharpSqlEntity).FullName }].")
        { }

        public InvalidJoinException(MethodInfo methodInfo)
          : base($"Unsupprted method call for [{ methodInfo.Name }], expected [{ nameof(SharpSqlEntity.Left) }] or [{ nameof(SharpSqlEntity.Inner) }].")
        { }
    }
}
