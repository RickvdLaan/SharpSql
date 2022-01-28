using System;

namespace SharpSql.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public sealed class SharpSqlForeignKeyAttribute : Attribute
{
    internal Type Relation { get; set; }

    public SharpSqlForeignKeyAttribute(Type relation)
    {
        Relation = relation;
    }
}