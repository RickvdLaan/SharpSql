using System;

namespace ORM.Attributes
{
    public sealed class ORMTableAttribute : Attribute
    {
        public string TableName { get { return CollectionType.Name; } }

        public Type CollectionType { get; private set; }

        public Type CollectionTypeLeft { get; private set; }

        public Type CollectionTypeRight { get; private set; }

        public Type EntityType { get; private set; }

        public ORMTableAttribute(Type collectionType, Type entityType)
        {
            // Example usage:
            // Users (CollectionType)
            // User (EntityType)
            // 
            // ORMTable(typeof(Users), typeof(User))

            if (!entityType.IsSubclassOf(typeof(ORMEntity)))
            {
                throw new ArgumentException();
            }

            CollectionType = collectionType;
            EntityType = entityType;
        }

        public ORMTableAttribute(Type collectionType, Type entityType, Type collectionTypeLeft, Type collectionTypeRight)
            : this(collectionType, entityType)
        {
            // Example usage:
            // Users->User (CollectionTypeLeft)
            // UserRoles (CollectionType)-> UserRole (EntityType)
            // Roles->Role (CollectionTypeRight)
            // 
            // ORMTable(nameof(UserRoles), typeof(UserRole), typeof(Users), typeof(Roles)

            CollectionTypeLeft = collectionTypeLeft;
            CollectionTypeRight = collectionTypeRight;
        }
    }
}
