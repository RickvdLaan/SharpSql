using System;

namespace ORM.Exceptions
{
    [Serializable]
    public class ORMPropertyNotNullableException : Exception
    {
        public ORMPropertyNotNullableException()
        { }

        public ORMPropertyNotNullableException(string message)
            : base(message)
        { }

        public ORMPropertyNotNullableException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
