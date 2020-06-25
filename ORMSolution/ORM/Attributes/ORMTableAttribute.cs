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
            if (!entityType.IsSubclassOf(typeof(ORMEntity)))
            {
                throw new ArgumentException();
            }

            CollectionType = collectionType;
            EntityType = entityType;
        }

        public ORMTableAttribute(Type collectionType, Type collectionTypeLeft, Type collectionTypeRight)
        {
            // Example usage:
            // Users->User (CollectionTypeLeft)
            // UserRole (CollectionType)
            // Roles->Role (CollectionTypeRight)
            // 
            // ORMTable(nameof(UserRole), typeof(Users), typeof(Roles)

            CollectionType = collectionType;
            CollectionTypeLeft = collectionTypeLeft;
            CollectionTypeRight = collectionTypeRight;
        }
    }
}
