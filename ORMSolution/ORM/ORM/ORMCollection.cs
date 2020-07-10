using ORM.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ORM
{
    [Serializable]
    public class ORMCollection<T> : IEnumerable<ORMEntity> where T : ORMEntity
    {
        public string ExecutedQuery { get; internal set; } = "An unknown query has been executed.";

        internal Expression<Func<T, object>> SelectExpression { get; set; }

        internal Expression<Func<T, bool>> WhereExpression { get; set; }

        internal Expression<Func<T, object>> SortExpression { get; set; }

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
            using (var connection = new SQLConnection())
            {
                var sqlBuilder = new SQLBuilder();

                sqlBuilder.BuildQuery(TableAttribute, SelectExpression, WhereExpression, SortExpression, maxNumberOfItemsToReturn);

                connection.ExecuteCollectionQuery(this, sqlBuilder);

                ExecutedQuery = sqlBuilder.GeneratedQuery;
            }
        }

        public void Select(Expression<Func<T, object>> expression)
        {
            SelectExpression = expression;
        }

        public void Where(Expression<Func<T, bool>> expression)
        {
            WhereExpression = expression;
        }

        public void OrderBy(Expression<Func<T, object>> expression)
        {
            SortExpression = expression;
        }
    }
}