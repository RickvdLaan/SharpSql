using SharpSql.Attributes;
using SharpSql.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;

namespace SharpSql;

[Serializable]
public class SharpSqlCollection<EntityType> : ISharpSqlCollection<EntityType>, IEnumerable<EntityType> where EntityType : SharpSqlEntity
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
    /// Gets the number of elements contained in the <see cref="SharpSqlCollection{EntityType}"/>.
    /// </summary>
    public int Count => MutableEntityCollection.Count;

    /// <summary>
    /// Gets whether change tracking is enabled or disabled; it's <see langword="false"/> by <see langword="default"/>.
    /// </summary>
    public bool DisableChangeTracking { get; internal set; } = false;

    /// <summary>
    /// Gets the table scheme of the current collection of <see cref="SharpSqlEntity"/> objects.
    /// </summary>
    public ReadOnlyCollection<string> TableScheme => SharpSqlUtilities.CachedColumns[GetType()].Keys.ToList().AsReadOnly(); // Ouch performance!

    /// <summary>
    /// Gets a read-only collection of fetched <see cref="SharpSqlEntity"/> entities.
    /// </summary>
    public ReadOnlyCollection<EntityType> EntityCollection => MutableEntityCollection.AsReadOnly();

    internal SharpSqlTableAttribute TableAttribute => (SharpSqlTableAttribute)Attribute.GetCustomAttribute(GetType(), typeof(SharpSqlTableAttribute));

    internal List<EntityType> MutableEntityCollection { get; set; } = new List<EntityType>();

    private Expression<Func<EntityType, object>> SelectExpression { get; set; }

    private Expression<Func<EntityType, object>> JoinExpression { get; set; }

    private Expression InternalJoinExpression { get; set; }

    private Expression<Func<EntityType, bool>> WhereExpression { get; set; }

    private Expression InternalWhereExpression { get; set; }

    private Expression<Func<EntityType, object>> SortExpression { get; set; }

    public SharpSqlCollection() { }

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
                return MutableEntityCollection[index];

            return null;
        }
    }

    /// <summary>
    /// Fetches all the records from the database.
    /// </summary>
    public ISharpSqlCollection<EntityType> Fetch()
    {
        return Fetch(-1);
    }

    /// <summary>
    /// Fetches a maximum number records from the database.
    /// </summary>
    /// <param name="maxNumberOfItemsToReturn">The maximum number of records to return.</param>
    public ISharpSqlCollection<EntityType> Fetch(long maxNumberOfItemsToReturn)
    {
        return Fetch(null, maxNumberOfItemsToReturn);
    }

    /// <summary>
    /// Returns the query for the current expression.
    /// </summary>
    /// <param name="maxNumberOfItemsToReturn">The maximum number of records to return.</param>
    /// <returns></returns>
    public QueryBuilder ToQuery(long maxNumberOfItemsToReturn = -1)
    {
        var queryBuilder = new QueryBuilder();

        queryBuilder.BuildQuery(TableAttribute, SelectExpression, JoinExpression ?? InternalJoinExpression, WhereExpression ?? InternalWhereExpression, SortExpression, maxNumberOfItemsToReturn);

        return queryBuilder;
    }

    /// <summary>
    /// Saves the changes made to the database.
    /// </summary>
    public void SaveChanges()
    {
        // -Rick, 25 September 2020
        for (int i = 0; i < MutableEntityCollection.Count; i++)
        {
            switch (MutableEntityCollection[i].ObjectState)
            {
                case ObjectState.New:
                case ObjectState.Untracked:
                    MutableEntityCollection[i].Save();
                    break;
                case ObjectState.Fetched:
                    if (MutableEntityCollection[i].IsDirty)
                        MutableEntityCollection[i].Save();
                    break;
                case ObjectState.Record:
                    continue;
                case ObjectState.ScheduledForDeletion:
                    if (MutableEntityCollection[i].IsMarkedAsDeleted)
                        throw new Exception("Obj has already been deleted");
                    else
                    {
                        MutableEntityCollection[i].Save();
                        PhysicalRemove(MutableEntityCollection[i]);
                    }
                    break;
                default:
                case ObjectState.Unset:
                    // Todo custom exception, should be invalid and impossible.
                    throw new Exception("Unset?");
            }
        }
    }

    /// <summary>
    /// Adds the provided <see cref="SharpSqlEntity"/> to the current collection.
    /// </summary>
    /// <param name="entity">The <see cref="SharpSqlEntity"/> to be added.</param>
    public void Add(EntityType entity)
    {
        MutableEntityCollection.Add(entity);
    }

    /// <summary>
    /// Marks the provided <see cref="SharpSqlEntity"/> to be deleted.
    /// </summary>
    /// <param name="entity">The <see cref="SharpSqlEntity"/> to be deleted.</param>
    public void Remove(EntityType entity)
    {
        MutableEntityCollection.Find(x => x.Equals(entity)).ScheduleForDeletion();
    }

    internal void PhysicalRemove(EntityType entity)
    {
        MutableEntityCollection.Remove(entity);
    }

    /// <summary>
    /// Returns the amount of records in the database for the current table.
    /// </summary>
    /// <returns>The record count in the database for the current table</returns>
    public static int Records()
    {
        return (int)DatabaseUtilities.ExecuteDirectQuery(QueryBuilder.Count(new SharpSqlTableAttribute(SharpSqlUtilities.CollectionEntityRelations[typeof(EntityType)], typeof(EntityType)))).Rows[0].ItemArray[0];
    }

    #region IEnumerable<SharpSqlEntity>

    /// <summary>
    /// Returns an enumerator that iterates through the <see cref="SharpSqlCollection{EntityType}"/>.
    /// </summary>
    /// <returns>A <see cref="List{EntityType}.Enumerator"/>.</returns>
    public IEnumerator<EntityType> GetEnumerator()
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
    /// <returns>Returns the current instance of <see cref="SharpSqlCollection{EntityType}"/>.</returns>
    public SharpSqlCollection<EntityType> Select(Expression<Func<EntityType, object>> expression)
    {
        SelectExpression = expression ?? throw new ArgumentNullException(nameof(expression));

        return this;
    }

    /// <summary>
    /// Sets the join expression; not mentioning a join-type automatically uses the left join.
    /// </summary>
    /// <param name="expression">The join expression.</param>
    /// <exception cref="ArgumentNullException">When setting an expression; the argument cannot be left null.</exception>
    /// <returns>Returns the current instance of <see cref="SharpSqlCollection{EntityType}"/>.</returns>
    public SharpSqlCollection<EntityType> Join(Expression<Func<EntityType, object>> expression)
    {
        JoinExpression = expression ?? throw new ArgumentNullException(nameof(expression));

        return this;
    }

    /// <summary>
    /// Sets the where expression used to filter records.
    /// </summary>
    /// <param name="expression">The where expression.</param>
    /// <exception cref="ArgumentNullException">When setting an expression; the argument cannot be left null.</exception>
    /// <returns>Returns the current instance of <see cref="SharpSqlCollection{EntityType}"/>.</returns>
    public SharpSqlCollection<EntityType> Where(Expression<Func<EntityType, bool>> expression)
    {
        WhereExpression = expression ?? throw new ArgumentNullException(nameof(expression));

        return this;
    }

    /// <summary>
    /// Sets the order by expression used to sort the result-set in ascending or descending order.
    /// </summary>
    /// <param name="expression">The order by expression.</param>
    /// <exception cref="ArgumentNullException">When setting an expression; the argument cannot be left null.</exception>
    /// <returns>Returns the current instance of <see cref="SharpSqlCollection{EntityType}"/>.</returns>
    public SharpSqlCollection<EntityType> OrderBy(Expression<Func<EntityType, object>> expression)
    {
        SortExpression = expression ?? throw new ArgumentNullException(nameof(expression));

        return this;
    }

    internal ISharpSqlCollection<EntityType> Fetch(SharpSqlEntity entity, long maxNumberOfItemsToReturn, Expression internalEntityJoinExpression = null)
    {
        var queryBuilder = new QueryBuilder();

        queryBuilder.BuildQuery(TableAttribute, SelectExpression, JoinExpression ?? InternalJoinExpression ?? internalEntityJoinExpression, WhereExpression ?? InternalWhereExpression, SortExpression, maxNumberOfItemsToReturn);

        if (ExecutedQuery.Equals(queryBuilder.GeneratedQuery, StringComparison.InvariantCultureIgnoreCase))
            return this;

        if (entity == null)
            QueryExecuter.ExecuteCollectionQuery(this, queryBuilder);
        else
            QueryExecuter.ExecuteEntityQuery(entity, queryBuilder);

        ExecutedQuery = queryBuilder.GeneratedQuery;

        return this;
    }

    internal SharpSqlCollection<EntityType> InternalJoin(Expression expression)
    {
        InternalJoinExpression = expression ?? throw new ArgumentNullException(nameof(expression));

        return this;
    }

    internal SharpSqlCollection<EntityType> InternalWhere(BinaryExpression expression)
    {
        InternalWhereExpression = expression ?? throw new ArgumentNullException(nameof(expression));

        return this;
    }

    /// <summary>
    /// The INNER JOIN keyword for many-to-many tables.
    /// </summary>
    public SharpSqlCollection<EntityType> Inner() => default;

    /// <summary>
    /// The LEFT JOIN keyword for many-to-many tables.
    /// </summary>
    public SharpSqlCollection<EntityType> Left() => default;
}