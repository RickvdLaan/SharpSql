using System;

namespace SharpSql.Exceptions
{
    [Serializable]
    public class ORMIllegalUniqueConstraintException : Exception
    {
        public ORMIllegalUniqueConstraintException(string columnName)
           : base($"Column [{ columnName }] is not a unique constraint.")
        { }
    }
}
