using ORM.Attributes;
using ORM.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace ORM
{
    [Serializable]
    public class ORMCollection<EntityType> : IORMCollection, IEnumerable<ORMEntity> where EntityType : ORMEntity
    {
        public string ExecutedQuery { get; internal set; } = "An unknown query has been executed.";

        public bool DisableChangeTracking { get; set; }

        public ReadOnlyCollection<string> TableScheme => ORMUtilities.CachedColumns[GetType()].AsReadOnly();

        internal Expression<Func<EntityType, object>> SelectExpression { get; set; }

        internal Expression<Func<EntityType, object>> JoinExpression { get; set; }

        internal Expression InternalJoinExpression { get; set; }

        internal Expression<Func<EntityType, bool>> WhereExpression { get; set; }

        internal Expression InternalWhereExpression { get; set; }

        internal Expression<Func<EntityType, object>> SortExpression { get; set; }

        internal ORMTableAttribute TableAttribute
        { 
            get { return (ORMTableAttribute)Attribute.GetCustomAttribute(GetType(), typeof(ORMTableAttribute)); }
        }

        internal List<EntityType> _collection = new List<EntityType>();
        public List<EntityType> Collection
        {
            get { return _collection; }
            internal set { _collection = value; }
        }

        public ORMCollection()
        {
            Collection = new List<EntityType>();
        }

        internal void Add(EntityType entity)
        {
            Collection.Add(entity);
        }

        public ORMEntity this[int index]
        {
            get { return Collection[index]; }
            set { Collection.Insert(index, value as EntityType); }
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

        public static int Records()
        {
            return (int)ORMUtilities.ExecuteDirectQuery(new SQLBuilder().Count(new ORMTableAttribute(ORMUtilities.CollectionEntityRelations[typeof(EntityType)], typeof(EntityType)))).Rows[0].ItemArray[0];
        }

        public void Fetch(long maxNumberOfItemsToReturn)
        {
            Fetch(null, maxNumberOfItemsToReturn);
        }

        internal void Fetch(ORMEntity entity, long maxNumberOfItemsToReturn)
        {
            var sqlBuilder = new SQLBuilder();

            sqlBuilder.BuildQuery(TableAttribute, SelectExpression, JoinExpression ?? InternalJoinExpression, WhereExpression ?? InternalWhereExpression, SortExpression, maxNumberOfItemsToReturn);

            if (ExecutedQuery == sqlBuilder.GeneratedQuery)
                return;

            if (entity == null)
                SQLExecuter.ExecuteCollectionQuery(this, sqlBuilder);
            else
                SQLExecuter.ExecuteEntityQuery(entity, sqlBuilder);

            ExecutedQuery = sqlBuilder.GeneratedQuery;
        }

        public ORMCollection<EntityType> Select(Expression<Func<EntityType, object>> expression)
        {
            SelectExpression = expression ?? throw new ArgumentNullException();

            return this;
        }

        public ORMCollection<EntityType> Join(Expression<Func<EntityType, object>> expression)
        {
            JoinExpression = expression ?? throw new ArgumentNullException();

            return this;
        }

        internal ORMCollection<EntityType> InternalJoin(Expression expression)
        {
            InternalJoinExpression = expression ?? throw new ArgumentNullException();

            return this;
        }

        public ORMCollection<EntityType> Where(Expression<Func<EntityType, bool>> expression)
        {
            WhereExpression = expression ?? throw new ArgumentNullException();

            return this;
        }

        internal ORMCollection<EntityType> InternalWhere(BinaryExpression expression)
        {
            InternalWhereExpression = expression ?? throw new ArgumentNullException();

            return this;
        }

        public ORMCollection<EntityType> OrderBy(Expression<Func<EntityType, object>> expression)
        {
            SortExpression = expression ?? throw new ArgumentNullException();

            return this;
        }

        public ORMCollection<EntityType> Inner() => default;

        public ORMCollection<EntityType> Left() => default;

        public ORMCollection<EntityType> Right() => default;

        public ORMCollection<EntityType> Full() => default;
    }
}