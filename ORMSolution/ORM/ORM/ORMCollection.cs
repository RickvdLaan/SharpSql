using Microsoft.Data.SqlClient;
using ORM.Attributes;
using ORM.SQL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

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

        private string _getQuery = null;
        public string GetQuery
        {
            get { return _getQuery.ToUpper(); }
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
            ORMTableAttribute tableAttribute = (ORMTableAttribute)Attribute.GetCustomAttribute(GetType(), typeof(ORMTableAttribute));

            using (SQLConnection connection = new SQLConnection())
            {
                var sqlBuilder = new SQLBuilder();

                sqlBuilder.BuildQuery(tableAttribute, maxNumberOfItemsToReturn);
                _getQuery = sqlBuilder.ToString();

                connection.ExecuteCollectionQuery(ref _collection, sqlBuilder, tableAttribute);
            }
        }

        public void Where(Expression<Func<T, bool>> field, long maxNumberOfItemsToReturn = -1)
        {
            ORMTableAttribute tableAttribute = (ORMTableAttribute)Attribute.GetCustomAttribute(GetType(), typeof(ORMTableAttribute));

            using (SQLConnection connection = new SQLConnection())
            {
                var sqlBuilder = new SQLBuilder();

                sqlBuilder.BuildQuery(field.Body, tableAttribute, maxNumberOfItemsToReturn);
                _getQuery = sqlBuilder.ToString();

                connection.ExecuteCollectionQuery(ref _collection, sqlBuilder, tableAttribute);
            }
        }
    }
}