using ORM.Interfaces;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace ORM
{
    public class ORMEntity : ORMObject, IORMEntity
    {
        public string ExecutedQuery { get; internal set; } = "An unknown query has been executed.";

        private string InternalPkName { get; set; }

        protected ORMEntity() { }

        protected ORMEntity(string pkName)
        {
            InternalPkName = pkName;
        }

        protected EntityType FetchEntityById<CollectionType, EntityType>(object id)
            where CollectionType : ORMCollection<EntityType>, new()
            where EntityType : ORMEntity, new()
        {
            var property = typeof(EntityType).GetProperties().Where(x => x.Name == InternalPkName).FirstOrDefault();

            if (property == null)
            {
                throw new ArgumentException($"No PK-property found for name: \"{InternalPkName}\" in {typeof(EntityType).Name}.");
            }

            var left = Expression.Property(Expression.Parameter(typeof(EntityType), $"x"), property);
            var right = Expression.Constant(id, id.GetType());

            var collection = new CollectionType();
            collection.InternalWhere(Expression.Equal(left, right));
            collection.Fetch(1);

            ExecutedQuery = collection.ExecutedQuery;

            return (EntityType)collection[0];
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