using System;

namespace SharpSql.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public sealed class SharpSqlColumnAttribute : Attribute
{
    public string ColumnName { get; private set; }

    public SharpSqlColumnAttribute(string columnName)
    {
        if (string.IsNullOrEmpty(columnName))
            throw new ArgumentNullException(nameof(columnName));

        ColumnName = columnName;
    }
}