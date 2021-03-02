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
        private string _executedQuery = string.Empty;
        /// <summary>
        /// Gets the executed query or returns <see cref="string.Empty"/>.
        /// </summary>
        public string ExecutedQuery
        {
            get { return _executedQuery.ToUpperInvariant(); }
            internal set { _executedQuery = value; }
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="ORMCollection{EntityType}"/>.
        /// </summary>
        public int Count => MutableEntityCollection.Count;

        /// <summary>
        /// Gets whether change tracking is enabled or disabled; it's <see langword="false"/> by <see langword="default"/>.
        /// </summary>
        public bool DisableChangeTracking { get; internal set; } = false;

        /// <summary>
        /// Gets the table scheme of the current collection of <see cref="ORMEntity"/> objects.
        /// </summary>
        public ReadOnlyCollection<string> TableScheme => ORMUtilities.CachedColumns[GetType()].AsReadOnly();

        /// <summary>
        /// Gets a read-only collection of fetched <see cref="ORMEntity"/> entities.
        /// </summary>
        public ReadOnlyCollection<ORMEntity> EntityCollection => MutableEntityCollection.AsReadOnly();

        internal ORMTableAttribute TableAttribute => (ORMTableAttribute)Attribute.GetCustomAttribute(GetType(), typeof(ORMTableAttribute));

        internal List<ORMEntity> MutableEntityCollection { get; set; } = new List<ORMEntity>();

        private Expression<Func<EntityType, object>> SelectExpression { get; set; }

        private Expression<Func<EntityType, object>> JoinExpression { get; set; }

        private Expression InternalJoinExpression { get; set; }

        private Expression<Func<EntityType, bool>> WhereExpression { get; set; }

        private Expression InternalWhereExpression { get; set; }

        private Expression<Func<EntityType, object>> SortExpression { get; set; }

        public ORMCollection() { }

        /// <summary>
        /// Returns a entity from the fetched collection based on the index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public EntityType this[int index]
        {
            get 
            {
                if (index < MutableEntityCollection.Count)
                    return MutableEntityCollection[index] as EntityType;

                return null;
            }
        }

        /// <summary>
        /// Fetches all the records from the database.
        /// </summary>
        public void Fetch()
        {
            Fetch(-1);
        }

        /// <summary>
        /// Fetches a maximum number records from the database.
        /// </summary>
        /// <param name="maxNumberOfItemsToReturn">The maximum number of records to return.</param>
        public void Fetch(long maxNumberOfItemsToReturn)
        {
            Fetch(null, maxNumberOfItemsToReturn);
        }

        /// <summary>
        /// Saves the changes made to the database.
        /// </summary>
        public void SaveChanges()
        {
            // @Todo:
            // A naive approach - but it works for now. We probably want to create some kind of state
            // for objects and add batch execution.
            // -Rick, 25 September 2020
            foreach (var entity in MutableEntityCollection)
            {
                if (entity.IsMarkAsDeleted)
                {
                    entity.Delete();
                }
                else if (entity.IsDirty)
                {
                    entity.Save();
                }
            }
        }

        /// <summary>
        /// Adds the provided <see cref="ORMEntity"/> to the current collection.
        /// </summary>
        /// <param name="entity">The <see cref="ORMEntity"/> to be added.</param>
        public void Add(ORMEntity entity)
        {
            MutableEntityCollection.Add(entity);
        }

        /// <summary>
        /// Marks the provided <see cref="ORMEntity"/> to be deleted.
        /// </summary>
        /// <param name="entity">The <see cref="ORMEntity"/> to be deleted.</param>
        public void Remove(ORMEntity entity)
        {
            MutableEntityCollection.Find(x => x.Equals(entity)).IsMarkAsDeleted = true;
        }

        /// <summary>
        /// Returns the amount of records in the database for the current table.
        /// </summary>
        /// <returns>The record count in the database for the current table</returns>
        public static int Records()
        {
            return (int)DatabaseUtilities.ExecuteDirectQuery(new SQLBuilder().Count(new ORMTableAttribute(ORMUtilities.CollectionEntityRelations[typeof(EntityType)], typeof(EntityType)))).Rows[0].ItemArray[0];
        }

        #region IEnumerable<ORMEntity>

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="ORMCollection{EntityType}"/>.
        /// </summary>
        /// <returns>A <see cref="List{EntityType}.Enumerator"/>.</returns>
        public IEnumerator<ORMEntity> GetEnumerator()
        {
            return MutableEntityCollection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        /// <summary>
        /// <para>Sets the select expression to select a specific amount of columns; <see langword="default"/> is all columns.</para>
        /// <para>By not setting the select expression the <see langword="default"/> is used.</para>
        /// </summary>
        /// <param name="expression">The select expression.</param>
        /// <exception cref="ArgumentNullException">When setting an expression; the argument cannot be left null.</exception>
        /// <returns>Returns the current instance of <see cref="ORMCollection{EntityType}"/>.</returns>
        public ORMCollection<EntityType> Select(Expression<Func<EntityType, object>> expression)
        {
            SelectExpression = expression ?? throw new ArgumentNullException();

            return this;
        }

        /// <summary>
        /// Sets the join expression; not mentioning a join-type automatically uses the left join.
        /// </summary>
        /// <param name="expression">The join expression.</param>
        /// <exception cref="ArgumentNullException">When setting an expression; the argument cannot be left null.</exception>
        /// <returns>Returns the current instance of <see cref="ORMCollection{EntityType}"/>.</returns>
        public ORMCollection<EntityType> Join(Expression<Func<EntityType, object>> expression)
        {
            JoinExpression = expression ?? throw new ArgumentNullException();

            return this;
        }

        /// <summary>
        /// Sets the where expression used to filter records.
        /// </summary>
        /// <param name="expression">The where expression.</param>
        /// <exception cref="ArgumentNullException">When setting an expression; the argument cannot be left null.</exception>
        /// <returns>Returns the current instance of <see cref="ORMCollection{EntityType}"/>.</returns>
        public ORMCollection<EntityType> Where(Expression<Func<EntityType, bool>> expression)
        {
            WhereExpression = expression ?? throw new ArgumentNullException();

            return this;
        }

        /// <summary>
        /// Sets the order by expression used to sort the result-set in ascending or descending order.
        /// </summary>
        /// <param name="expression">The order by expression.</param>
        /// <exception cref="ArgumentNullException">When setting an expression; the argument cannot be left null.</exception>
        /// <returns>Returns the current instance of <see cref="ORMCollection{EntityType}"/>.</returns>
        public ORMCollection<EntityType> OrderBy(Expression<Func<EntityType, object>> expression)
        {
            SortExpression = expression ?? throw new ArgumentNullException();

            return this;
        }

        internal void Fetch(ORMEntity entity, long maxNumberOfItemsToReturn, Expression internalEntityJoinExpression = null)
        {
            var sqlBuilder = new SQLBuilder();

            sqlBuilder.BuildQuery(TableAttribute, SelectExpression, JoinExpression ?? InternalJoinExpression ?? internalEntityJoinExpression, WhereExpression ?? InternalWhereExpression, SortExpression, maxNumberOfItemsToReturn);

            if (ExecutedQuery.Equals(sqlBuilder.GeneratedQuery, StringComparison.InvariantCultureIgnoreCase))
                return;

            if (entity == null)
                SQLExecuter.ExecuteCollectionQuery(this, sqlBuilder);
            else
                SQLExecuter.ExecuteEntityQuery(entity, sqlBuilder);

            ExecutedQuery = sqlBuilder.GeneratedQuery;
        }

        internal ORMCollection<EntityType> InternalJoin(Expression expression)
        {
            InternalJoinExpression = expression ?? throw new ArgumentNullException();

            return this;
        }

        internal ORMCollection<EntityType> InternalWhere(BinaryExpression expression)
        {
            InternalWhereExpression = expression ?? throw new ArgumentNullException();

            return this;
        }

        /// <summary>
        /// The INNER JOIN keyword for many-to-many tables.
        /// </summary>
        public ORMCollection<EntityType> Inner() => default;

        /// <summary>
        /// The LEFT JOIN keyword for many-to-many tables.
        /// </summary>
        public ORMCollection<EntityType> Left() => default;
    }
}