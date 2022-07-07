using System;

namespace SharpSql.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public sealed class SharpSqlVirtualForeignKeyAttribute : Attribute
{
    internal string PropertyLink { get; set; }

    public SharpSqlVirtualForeignKeyAttribute(string propertyLink)
    {
        PropertyLink = propertyLink;
    }
}
