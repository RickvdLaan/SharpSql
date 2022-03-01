using System;

namespace SharpSql.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public sealed class SharpSqlUniqueConstraintAttribute : Attribute
{
    public SharpSqlUniqueConstraintAttribute() { }
}