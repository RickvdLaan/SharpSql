using System;

namespace ORM.Attributes
{
    public sealed class ORMTableAttribute : Attribute
    {
        public ORMTableAttribute(string tableName, Type entityType)
        {
            if (!entityType.IsSubclassOf(typeof(ORMEntity)))
            {
                throw new ArgumentException();
            }
        }
    }
}
