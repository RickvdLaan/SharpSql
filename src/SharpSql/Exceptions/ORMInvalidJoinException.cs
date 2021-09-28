using System;
using System.Reflection;

namespace SharpSql.Exceptions
{
    [Serializable]
    public class ORMInvalidJoinException : Exception
    {
        public ORMInvalidJoinException(string customMessage)
            : base(customMessage)
        { }

        public ORMInvalidJoinException(PropertyInfo propertyInfo)
           : base($"Cannot join field [{ propertyInfo.Name }] of type [{propertyInfo.PropertyType}], since it's not of type [{ typeof(ORMEntity).FullName }].")
        { }

        public ORMInvalidJoinException(MethodInfo methodInfo)
          : base($"Unsupprted method call for [{ methodInfo.Name }], expected [{ nameof(ORMEntity.Left) }] or [{ nameof(ORMEntity.Inner) }].")
        { }
    }
}
