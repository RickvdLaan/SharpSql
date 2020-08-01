using System;

namespace ORM.Attributes
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
