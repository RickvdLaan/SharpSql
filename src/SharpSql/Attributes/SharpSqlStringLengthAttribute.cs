using System;

namespace SharpSql.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public sealed class SharpSqlStringLengthAttribute : Attribute
{
    public int Length { get; private set; }

    public SharpSqlStringLengthAttribute(int length)
    {
        Length = length;
    }
}