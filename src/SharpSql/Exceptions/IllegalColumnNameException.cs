using System;

namespace SharpSql.Exceptions
{
    [Serializable]
    public class IllegalColumnNameException : Exception
    {
        public IllegalColumnNameException()
        { }

        public IllegalColumnNameException(string message)
            : base(message)
        { }

        public IllegalColumnNameException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
