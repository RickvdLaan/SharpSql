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
        internal List<ORMEntity> _collection;
        internal List<ORMEntity> Collection
        {
            get { return _collection; }
            set { _collection = value; }
        }

        internal string _getQuery;
        public string GetQuery
        {
            get { return _getQuery.ToUpper(); }
            internal set { _getQuery = value; }
        }

        public ORMCollection()
        {
            Collection = new List<ORMEntity>();
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
            ORMTableAttribute attribute = (ORMTableAttribute)Attribute.GetCustomAttribute(GetType(), typeof(ORMTableAttribute));

            using (SQLBuilder sqlBuilder = new SQLBuilder())
            {
                sqlBuilder.ExecuteCollectionQuery(ref _collection, ref _getQuery, attribute, maxNumberOfItemsToReturn);
            }
        }

        public void Where<TField>(Expression<Func<T, TField>> field, TField value, long maxNumberOfItemsToReturn = -1)
        {
            ORMTableAttribute attribute = (ORMTableAttribute)Attribute.GetCustomAttribute(GetType(), typeof(ORMTableAttribute));
            SQLClauseBuilder<T> clauseBuilder = new SQLClauseBuilder<T>();

            using (SQLBuilder sqlBuilder = new SQLBuilder())
            {
                sqlBuilder.AddSQLClauses(clauseBuilder.Where(field, value));

                sqlBuilder.ExecuteCollectionQuery(ref _collection, ref _getQuery, attribute, maxNumberOfItemsToReturn);
            }
        }
    }
}