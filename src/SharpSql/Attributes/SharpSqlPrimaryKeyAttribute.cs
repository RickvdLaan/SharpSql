using System;

namespace SharpSql.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public sealed class SharpSqlPrimaryKeyAttribute : Attribute
{
    internal string PropertyName { get; set; }

    internal string ColumnName { get; set; }

    internal bool IsAutoIncrement { get; set; }

    public SharpSqlPrimaryKeyAttribute(bool isAutoIncrement = true)
    {
        IsAutoIncrement = isAutoIncrement;
    }
}