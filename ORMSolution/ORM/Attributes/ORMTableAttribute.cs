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

        public ORMTableAttribute(string tableName, ORMCollection<ORMEntity> left, ORMCollection<ORMEntity> right)
        {
            // Koppeltabel, voorbeeld:
            // Users->User
            // UserRole
            // Roles->Role
            // 
            // ORMTable(nameof(UserRole), typeof(Users), typeof(Roles)
        }
    }
}
