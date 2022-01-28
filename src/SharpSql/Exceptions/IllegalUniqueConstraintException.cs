using System;

namespace SharpSql.Exceptions;

[Serializable]
public class IllegalUniqueConstraintException : Exception
{
    public IllegalUniqueConstraintException(string columnName)
       : base($"Column [{ columnName }] is not a unique constraint.")
    { }
}