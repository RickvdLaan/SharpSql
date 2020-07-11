using ORM.Interfaces;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ORM
{
    public class ORMEntity : ORMObject, IORMEntity
    {
        public string ExecutedQuery { get; internal set; } = "An unknown query has been executed.";

        private string InternalPkName { get; set; }

        protected ORMEntity(string pkName)
        {
            InternalPkName = pkName;
        }

        internal PropertyInfo GetPrimaryKeyPropertyInfo()
        {
            if (string.IsNullOrWhiteSpace(InternalPkName))
            {
                throw new ArgumentNullException($"PK-property \"{InternalPkName}\" can't be null or empty.");
            }

            var propertyInfo = GetType().GetProperties().Where(x => x.Name == InternalPkName).FirstOrDefault();

            if (propertyInfo == null)
            {
                throw new ArgumentException($"No PK-property found for name: \"{InternalPkName}\" in {GetType().Name}.");
            }

            return propertyInfo;
        }

        protected void FetchEntityById<CollectionType, EntityType>(object id)
            where CollectionType : ORMCollection<EntityType>, new()
            where EntityType : ORMEntity, new()
        {
            var propertyInfo = GetPrimaryKeyPropertyInfo();

            var left = Expression.Property(Expression.Parameter(typeof(EntityType), $"x"), propertyInfo);
            var right = Expression.Constant(id, id.GetType());

            var collection = new CollectionType();
            collection.InternalWhere(Expression.Equal(left, right));
            collection.Fetch(this, 1);

            ExecutedQuery = collection.ExecutedQuery;
        }

        public virtual void Save()
        {
            throw new NotImplementedException();
        }

        public virtual void Delete()
        {
            throw new NotImplementedException();
        }
    }
}