using System;

namespace ORM.Attributes
{
    public sealed class ORMTableAttribute : Attribute
    {
        public string TableName { get; set; }

        public Type EntityType { get; set; }

        public ORMTableAttribute(string tableName, Type entityType)
        {
            if (!entityType.IsSubclassOf(typeof(ORMEntity)))
            {
                throw new ArgumentException();
            }

            TableName = tableName;
            EntityType = entityType;
        }
    }
}
