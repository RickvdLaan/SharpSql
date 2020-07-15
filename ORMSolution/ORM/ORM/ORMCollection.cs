using ORM.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ORM
{
    [Serializable]
    public class ORMCollection<EntityType> : IEnumerable<ORMEntity> where EntityType : ORMEntity
    {
        public string ExecutedQuery { get; internal set; } = "An unknown query has been executed.";

        public bool DisableChangeTracking { get; set; }

        internal Expression<Func<EntityType, object>> SelectExpression { get; set; }

        internal Expression<Func<EntityType, object>> JoinExpression { get; set; }

        internal Expression<Func<EntityType, bool>> WhereExpression { get; set; }

        internal Expression InternalWhereExpression { get; set; }

        internal Expression<Func<EntityType, object>> SortExpression { get; set; }

        internal ORMTableAttribute TableAttribute
        { 
            get { return (ORMTableAttribute)Attribute.GetCustomAttribute(GetType(), typeof(ORMTableAttribute)); }
        }

        internal List<ORMEntity> _collection;
        public List<ORMEntity> Collection
        {
            get { return _collection; }
            internal set { _collection = value; }
        }

        internal List<string> TableScheme => ORMUtilities.CachedColumns[GetType()];

        public ORMCollection()
        {
            Collection = new List<ORMEntity>();
        }

        internal void Add(ORMEntity entity)
        {
            Collection.Add(entity);
        }

        public ORMEntity this[int index]
        {
            get { return Collection[index]; }
            set { Collection.Insert(index, value); }
        }

        public IEnumerator<ORMEntity> GetEnumerator()
        {
            return Collection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Fetch()
        {
            Fetch(-1);
        }

        public void Fetch(long maxNumberOfItemsToReturn)
        {
            Fetch(null, maxNumberOfItemsToReturn);
        }

        internal void Fetch(ORMEntity entity, long maxNumberOfItemsToReturn)
        {
           using (var connection = new SQLConnection())
            {
                var sqlBuilder = new SQLBuilder();

                sqlBuilder.BuildQuery(TableAttribute, SelectExpression, JoinExpression, WhereExpression ?? InternalWhereExpression, SortExpression, maxNumberOfItemsToReturn);

                if (ExecutedQuery == sqlBuilder.GeneratedQuery)
                    return;

                if (entity == null)
                    connection.ExecuteCollectionQuery(this, sqlBuilder);
                else
                    connection.ExecuteEntityQuery(entity, sqlBuilder);

                ExecutedQuery = sqlBuilder.GeneratedQuery;
            }
        }

        public ORMCollection<EntityType> Select(Expression<Func<EntityType, object>> expression)
        {
            SelectExpression = expression;

            return this;
        }

        public ORMCollection<EntityType> Join(Expression<Func<EntityType, object>> expression)
        {
            JoinExpression = expression;

            return this;
        }

        public ORMCollection<EntityType> Where(Expression<Func<EntityType, bool>> expression)
        {
            WhereExpression = expression;

            return this;
        }

        internal ORMCollection<EntityType> InternalWhere(BinaryExpression expression)
        {
            InternalWhereExpression = expression;

            return this;
        }

        public ORMCollection<EntityType> OrderBy(Expression<Func<EntityType, object>> expression)
        {
            SortExpression = expression;

            return this;
        }
    }
}