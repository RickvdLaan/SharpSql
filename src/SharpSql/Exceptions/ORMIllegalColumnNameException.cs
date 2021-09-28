using System;

namespace SharpSql.Exceptions
{
    [Serializable]
    public class ORMIllegalColumnNameException : Exception
    {
        public ORMIllegalColumnNameException()
        { }

        public ORMIllegalColumnNameException(string message)
            : base(message)
        { }

        public ORMIllegalColumnNameException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
