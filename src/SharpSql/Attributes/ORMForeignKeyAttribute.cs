using System;

namespace SharpSql.Attributes
{
    public sealed class ORMForeignKeyAttribute : Attribute
    {
        internal Type Relation { get; set; }

        public ORMForeignKeyAttribute(Type relation)
        {
            Relation = relation;
        }
    }
}
