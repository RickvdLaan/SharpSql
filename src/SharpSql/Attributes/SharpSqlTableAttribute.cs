using System;

namespace SharpSql.Attributes
{
    public sealed class SharpSqlTableAttribute : Attribute
    {
        public string TableName { get { return CollectionType.Name; } }

        public Type CollectionType { get; private set; }

        public Type CollectionTypeLeft { get; private set; }

        public Type CollectionTypeRight { get; private set; }

        public Type EntityType { get; private set; }

        public SharpSqlTableAttribute(Type collectionType, Type entityType)
        {
            // Example usage:
            // Users (CollectionType)
            // User (EntityType)
            // 
            // SharpSqlTable(typeof(Users), typeof(User))

            if (!entityType.IsSubclassOf(typeof(SharpSqlEntity)))
            {
                throw new ArgumentException();
            }

            CollectionType = collectionType;
            EntityType = entityType;
        }

        public SharpSqlTableAttribute(Type collectionType, Type entityType, Type collectionTypeLeft, Type collectionTypeRight)
            : this(collectionType, entityType)
        {
            // Example usage:
            // Users->User (CollectionTypeLeft)
            // UserRoles (CollectionType)-> UserRole (EntityType)
            // Roles->Role (CollectionTypeRight)
            // 
            // SharpSqlTable(nameof(UserRoles), typeof(UserRole), typeof(Users), typeof(Roles)

            CollectionTypeLeft = collectionTypeLeft;
            CollectionTypeRight = collectionTypeRight;
        }
    }
}
