using System;

namespace SharpSql.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SharpSqlManyToManyAttribute : Attribute
    {
        public SharpSqlManyToManyAttribute() { }
    }
}