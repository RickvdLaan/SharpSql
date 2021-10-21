using System;

namespace SharpSql.Attributes
{
    public sealed class SharpSqlForeignKeyAttribute : Attribute
    {
        internal Type Relation { get; set; }

        public SharpSqlForeignKeyAttribute(Type relation)
        {
            Relation = relation;
        }
    }
}
