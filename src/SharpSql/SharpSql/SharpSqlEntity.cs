using Newtonsoft.Json;
using SharpSql.Attributes;
using SharpSql.Exceptions;
using SharpSql.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SharpSql;

[JsonConverter(typeof(EntityDeserializer))]
public class SharpSqlEntity : object, IEquatable<SharpSqlEntity>, ISharpSqlEntity
{
    #region Reflection helpers

    internal static BindingFlags PublicFlags => BindingFlags.Instance | BindingFlags.Public;

    internal static BindingFlags PublicIgnoreCaseFlags => PublicFlags | BindingFlags.IgnoreCase;

    internal static BindingFlags NonPublicFlags => BindingFlags.Instance | BindingFlags.NonPublic;

    #endregion

    private string _executedQuery = string.Empty;

    /// <summary>
    /// Gets the executed query or <see cref="string.Empty"/>.
    /// </summary>
    [JsonIgnore]
    public string ExecutedQuery
    {
        get { return _executedQuery.ToUpperInvariant(); }
        internal set { _executedQuery = value; }
    }

    private ObjectState _objectState = ObjectState.Unset;
    /// <summary>
    /// Gets the state of the entity.
    /// </summary>
    public ObjectState ObjectState
    {
        get
        {
            return _objectState;
        }
        internal set
        {
            if (ObjectState == ObjectState.Deleted)
                throw new InvalidOperationException();

            _objectState = value;
        }
    }

    /// <summary>
    /// Gets whether the <see cref="SharpSqlEntity"/> has an auto-increment primary key field.
    /// </summary>
    [JsonIgnore]
    public bool IsAutoIncrement { get { return PrimaryKey.Keys.Any(key => key.IsAutoIncrement); } }

    /// <summary>
    /// Gets whether the <see cref="ObjectState"/> is <see cref="ObjectState.New"/>.
    /// </summary>
    public bool IsNew
    {
        get
        {
            // Should never happen, but it's here in case it does happen. We can actually fix
            // the bug.
            if (ObjectState == ObjectState.Unset)
                throw new ArgumentException();

            return ObjectState == ObjectState.New;
        }
    }

    /// <summary>
    /// Gets whether the <see cref="ObjectState"/> is <see cref="ObjectState.Deleted"/>.
    /// </summary>
    public bool IsMarkedAsDeleted => ObjectState == ObjectState.Deleted;

    /// <summary>
    /// Gets whether change tracking is enabled or disabled, it's <see langword="false"/> by <see langword="default"/> and
    /// can be set to <see langword="true"/> through the <see cref="SharpSqlEntity"/> constructor.
    /// </summary>
    [JsonIgnore]
    public bool DisableChangeTracking { get; internal set; } = false;

    /// <summary>
    /// Gets the <see cref="SharpSqlPrimaryKey"/> of the current <see cref="SharpSqlEntity"/>.
    /// </summary>
    [JsonIgnore]
    public SharpSqlPrimaryKey PrimaryKey { get; private set; }

    /// <summary>
    /// Gets whether the value of a <see cref="SharpSqlEntity"/> has changed.
    /// </summary>
    public bool IsDirty
    {
        get
        {
            // A new object is always dirty (DirtyTracker is not doing anything in this case)
            if (IsNew)
                return true;
            // An object that has been deleted cannot be dirty.
            if (IsMarkedAsDeleted)
                return false;
            // An original fetched value is immutable; therefore it can't be dirty.
            if (ObjectState == ObjectState.OriginalFetchedValue)
                return false;
            // When a copy is made from the original value it becomes immutable; therefore it can't be
            // dirty regardless of what the DirtyTracker says based on its previous comparison.
            // Therefore it's always false.
            if (ObjectState == ObjectState.NewRecord)
                return false;

            UpdateIsDirtyList();

            return DirtyTracker.Any;
        }
    }

    /// <summary>
    /// Gets the table scheme from the current <see cref="SharpSqlEntity"/>.
    /// </summary>
    [JsonIgnore]
    public ReadOnlyCollection<string> TableScheme { get { return SharpSqlCache.EntityColumns[GetType()].Keys.ToList().AsReadOnly(); } } // Ouch performance!

    internal List<SharpSqlEntity> Relations { get; private set; } = new List<SharpSqlEntity>();

    internal DirtyTracker DirtyTracker { get; private set; }

    internal SharpSqlEntity OriginalFetchedValue { get; set; } = null;

    internal List<string> MutableTableScheme { get; private set; } = null;

    /// <summary>
    /// Initializes a new instance of <see cref="SharpSqlEntity"/> when deserializing.
    /// </summary>
    [JsonConstructor]
    private SharpSqlEntity(Type externalType)
    {
        ObjectState = ObjectState.ExternalRecord;

        InitializePrimaryKeys(externalType);
        InitializeMutableTableSchema(externalType);
        DirtyTracker = new DirtyTracker(MutableTableScheme);
    }

    /// <summary>
    /// Initializes a new instance of <see cref="SharpSqlEntity"/>.
    /// </summary>
    public SharpSqlEntity()
    {
        ObjectState = ObjectState.New;

        InitializePrimaryKeys();
        InitializeMutableTableSchema();
        DirtyTracker = new DirtyTracker(MutableTableScheme);
    }

    /// <summary>
    /// Initializes a new instance of <see cref="SharpSqlEntity"/>.
    /// </summary>
    /// <param name="disableChangeTracking">Enables or disables change tracking for the current entity.</param>
    public SharpSqlEntity(bool disableChangeTracking = false) : this()
    {
        DisableChangeTracking = disableChangeTracking;
    }

    private void InitializePrimaryKeys(Type externalType = null)
    {
        var attributes = new List<SharpSqlPrimaryKeyAttribute>();

        foreach (var property in externalType?.GetProperties() ?? GetType().GetProperties())
        {
            foreach (SharpSqlPrimaryKeyAttribute attribute in property.GetCustomAttributes(typeof(SharpSqlPrimaryKeyAttribute), true))
            {
                attribute.PropertyName = property.Name;
                attribute.ColumnName = (property.GetCustomAttributes(typeof(SharpSqlColumnAttribute), true).FirstOrDefault() as SharpSqlColumnAttribute)?.ColumnName ?? property.Name;
                attributes.Add(attribute);
            }
        }

        PrimaryKey = new SharpSqlPrimaryKey(attributes.Count);

        if (attributes.Count > 0)
        {
            foreach (var attribute in attributes)
            {
                PrimaryKey.Add(attribute.PropertyName, attribute.ColumnName, null, attribute.IsAutoIncrement);
            }
        }
        else
        {
            throw new PrimaryKeyAttributeNotImplementedException(GetType());
        }
    }

    private void InitializeMutableTableSchema(Type externalType = null)
    {
        if (SharpSqlCache.MutableColumns.ContainsKey(externalType ?? GetType()))
        {
            MutableTableScheme = SharpSqlCache.MutableColumns[externalType ?? GetType()];
            return;
        }

        MutableTableScheme = new List<string>(TableScheme.Count - PrimaryKey.Keys.Where(pk => pk.IsAutoIncrement).Count());

        foreach (var columnName in TableScheme)
        {
            if (PrimaryKey.Keys.Any(pk => pk.ColumnName == columnName && pk.IsAutoIncrement))
                continue;

            MutableTableScheme.Add(columnName);
        }

        SharpSqlCache.MutableColumns[externalType ?? GetType()] = MutableTableScheme;
    }

    /// <summary>
    /// Gets or sets the accessors of the current <see cref="SharpSqlEntity"/>.
    /// </summary>
    /// <param name="columnName">Provide the current column to be accessed.</param>
    /// <returns>Returns the provided column value.</returns>
    public object this[string columnName]
    {
        get
        {
            var propertyInfo = GetType().GetProperty(columnName, PublicIgnoreCaseFlags | NonPublicFlags);

            if (propertyInfo == null)
            {
                foreach (var property in GetType().GetProperties())
                {
                    // It's possible to have a different property name than sql column name. But it
                    // does come at a small performance cost.
                    var columnAttribute = property.GetCustomAttributes(typeof(SharpSqlColumnAttribute), false).FirstOrDefault() as SharpSqlColumnAttribute;

                    if (columnAttribute?.ColumnName == columnName)
                    {
                        return GetType().GetProperty(property.Name, PublicIgnoreCaseFlags | NonPublicFlags).GetValue(this);
                    }
                }

                throw new NotImplementedException($"The property [{columnName}] was not found in entity [{GetType().Name}].");
            }

            return propertyInfo.GetValue(this);
        }
        set
        {
            // @Todo: PK cannot be modified (except IsNew == true).

            var propertyInfo = GetType().GetProperty(columnName);

            if (propertyInfo == null)
            {
                foreach (var property in GetType().GetProperties())
                {
                    // It's possible to have a different property name than sql column name. But it
                    // does come at a small performance cost.
                    var columnAttribute = property.GetCustomAttributes(typeof(SharpSqlColumnAttribute), false).FirstOrDefault() as SharpSqlColumnAttribute;

                    if (columnAttribute?.ColumnName == columnName)
                    {
                        GetType().GetProperty(property.Name, PublicIgnoreCaseFlags | NonPublicFlags).SetValue(this, value);

                        UpdateIsDirtyList();

                        return;
                    }
                }

                throw new NotImplementedException($"The property [{columnName}] was not found in entity [{GetType().Name}].");
            }

            propertyInfo.SetValue(this, value);

            UpdateIsDirtyList();
        }
    }

    /// <summary>
    /// Returns a value indicating whether <see langword="this"/> instance is equal to a specified <see langword="object"/>.
    /// </summary>
    /// <param name="other">A <see langword="object"/> value to compare to <see langword="this"/> instance.</param>
    /// <returns><see langword="true"/> if 'other' has the same value as <see langword="this"/> instance; otherwise, <see langword="false"/>.</returns>
    public override bool Equals(object other)
    {
        return Equals(other as SharpSqlEntity);
    }

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A hash code for the current <see cref="SharpSqlEntity"/></returns>
    public override int GetHashCode()
    {
        var hashCode = new HashCode();

        for (int i = 0; i < MutableTableScheme.Count; i++)
        {
            hashCode.Add(this[MutableTableScheme[i]]?.GetHashCode());
        }

        hashCode.Add(IsDirty.GetHashCode());
        hashCode.Add(PrimaryKey.GetHashCode());

        return hashCode.ToHashCode();
    }

    /// <summary>
    /// Indicates whether two <see cref="SharpSqlEntity"/> objects are equal.
    /// </summary>
    /// <param name="leftSide">The first <see cref="SharpSqlEntity"/> to compare.</param>
    /// <param name="rightSide">The second <see cref="SharpSqlEntity"/> to compare.</param>
    /// <returns><see langword="true"/> if left is equal to right; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(SharpSqlEntity leftSide, SharpSqlEntity rightSide)
    {
        if (leftSide is null)
        {
            if (rightSide is null)
            {
                // null == null = true.
                return true;
            }

            // Only the left side is null.
            return false;
        }
        // Equals handles case of null on right side.
        return leftSide.Equals(rightSide);
    }

    /// <summary>
    ///  Indicates whether two <see cref="SharpSqlEntity"/> objects are not equal.
    /// </summary>
    /// <param name="leftSide">The first <see cref="SharpSqlEntity"/> to compare.</param>
    /// <param name="rightSide">The second <see cref="SharpSqlEntity"/> to compare.</param>
    /// <returns><see langword="true"/> if left is not equal to right; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(SharpSqlEntity leftSide, SharpSqlEntity rightSide)
    {
        return !(leftSide == rightSide);
    }

    /// <summary>
    /// Saves the current <see cref="SharpSqlEntity"/> (changes) to the database.
    /// </summary>
    public virtual void Save()
    {
        if (IsNew || ObjectState == ObjectState.ExternalRecord)
            PrimaryKey.Update(this);    

        if (IsMarkedAsDeleted)
            return;

        if (ObjectState == ObjectState.ScheduledForDeletion)
        {
            Delete();
            return;
        }

        if (PrimaryKey.IsEmpty)
            throw new EmptyPrimaryKeyException();
        
        if (IsDirty)
        {
            var queryBuilder = new QueryBuilder();

            // @Perfomance: it fixes some issues, but this is terrible for the performance.
            // Needs to be looked at! -Rick, 12 December 2020
            foreach (var column in MutableTableScheme)
            {
                // When a subEntity is not filled through the parent the PopulateChildEntity method
                // isn't called and therefore the subEntity is not added to the EntityRelations.
                if (this[column] != null && this[column].GetType().IsSubclassOf(typeof(SharpSqlEntity)))
                {
                    var subEntity = Activator.CreateInstance(this[column].GetType().UnderlyingSystemType) as SharpSqlEntity;

                    if (!Relations.Any(x => x.GetType() == subEntity.GetType()))
                    {
                        if ((this[column] as SharpSqlEntity).IsDirty
                         || (OriginalFetchedValue?[column] == null && !(this[column] as SharpSqlEntity).IsDirty))
                        {
                            Relations.Add(subEntity);
                        }
                    }
                }
            }

            foreach (var relation in Relations)
            {
                if (this[relation.GetType().Name] == null && OriginalFetchedValue[relation.GetType().Name] != null)
                {
                    continue;
                }
                else if ((this[relation.GetType().Name] as SharpSqlEntity).IsDirty)
                {
                    (this[relation.GetType().Name] as SharpSqlEntity).Save();
                }
            }

            if (IsNew || IsNew && Relations.Any(r => r.IsNew))
            {
                queryBuilder.BuildNonQuery(this, NonQueryType.Insert);

                var id = QueryExecuter.ExecuteNonQuery(queryBuilder);

                if (PrimaryKey.IsCombinedPrimaryKey)
                {
                    UpdateCombinedPrimaryKey();
                }
                else
                {
                    UpdateSinglePrimaryKey(id);
                }
            }
            else
            {
                queryBuilder.BuildNonQuery(this, NonQueryType.Update);
                QueryExecuter.ExecuteNonQuery(queryBuilder);
            }

            ExecutedQuery = queryBuilder.GeneratedQuery;
            OverrideOriginalFetchedValue();

            // We want to set the state after the override.
            ObjectState = ObjectState.Saved;

            // The changes to the object have been saved, therefore the IsDirtyTracker can be reset.
            DirtyTracker.Reset();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OverrideOriginalFetchedValue()
    {
        // Cases:
        // -> Update:
        // Changes have been saved to the database, therefore the original fetched value
        // can be overriden by the new record, so it can track changes again.

        // -> Insert:
        // Since the new object has been saved, it should have a original fetched value
        // to track its changes. But with the state NewRecord since it isn't fetched
        // from the database.

        GetType().GetProperty(nameof(OriginalFetchedValue), NonPublicFlags)
                 .SetValue(this, ShallowCopy(ObjectState.NewRecord));
    }

    internal void NonQuery<EntityType>(NonQueryType nonQueryType, object primaryKey, params (Expression<Func<EntityType, object>> Expression, object Value)[] columnValuePairs)
         where EntityType : SharpSqlEntity
    {
        // PK has been passed, therefore the object state has to be set to Record.
        ObjectState = ObjectState.Record;

        FetchEntityByPrimaryKey(primaryKey);

        this[PrimaryKey.Keys[0].ColumnName] = primaryKey;

        NonQuery(nonQueryType, columnValuePairs);
    }

    internal void NonQuery<EntityType>(NonQueryType nonQueryType, params (Expression<Func<EntityType, object>> Expression, object Value)[] columnValuePairs)
      where EntityType : SharpSqlEntity
    {
        if (columnValuePairs != null)
        {
            foreach (var columnValuePair in columnValuePairs)
            {
                var columnName = QueryBuilder.ParseUpdateExpression(columnValuePair.Expression);
                this[columnName] = columnValuePair.Value;

                if (ObjectState == ObjectState.ExternalRecord)
                {
                    MarkDirtyTrackerFieldsAs(true, columnName);
                }
            }
        }
        if (nonQueryType == NonQueryType.Delete)
        {
            Delete();
        }
        else
        {
            Save();
        }
    }

    /// <summary>
    /// Deletes the current <see cref="SharpSqlEntity"/> from the database.
    /// </summary>
    public virtual void Delete()
    {
        if (ObjectState == ObjectState.ExternalRecord)
            PrimaryKey.Update(this);

        if (PrimaryKey.IsEmpty)
            throw new EmptyPrimaryKeyException();

        ScheduleForDeletion();

        if (ObjectState == ObjectState.ScheduledForDeletion)
        {
            var queryBuilder = new QueryBuilder();
            queryBuilder.BuildNonQuery(this, NonQueryType.Delete);
            QueryExecuter.ExecuteNonQuery(queryBuilder);
            ExecutedQuery = queryBuilder.GeneratedQuery;
            ObjectState = ObjectState.Deleted;
        }
    }

    internal void ScheduleForDeletion()
    {
        if (!IsNew && ObjectState != ObjectState.ScheduledForDeletion && !IsMarkedAsDeleted)
        {
            ObjectState = ObjectState.ScheduledForDeletion;
        }
    }

    /// <summary>
    /// Fetches an <see cref="SharpSqlEntity"/> based on the provided primary key.
    /// </summary>
    /// <param name="primaryKey">The primary key.</param>
    /// <returns>Returns the fetched <see cref="SharpSqlEntity"/> or <see langword="null"/>.</returns>
    public SharpSqlEntity FetchEntityByPrimaryKey(object primaryKey)
    {
        PrimaryKey.Keys[0].Value = primaryKey;

        return FetchEntity(PrimaryKey);
    }

    /// <summary>
    /// Fetches an <see cref="SharpSqlEntity"/> based on the provided primary key and join(s).
    /// </summary>
    /// <typeparam name="EntityType"></typeparam>
    /// <param name="primaryKey">The primary key.</param>
    /// <param name="joins">The fields you want to join.</param>
    /// <returns>Returns the fetched <see cref="SharpSqlEntity"/> or <see langword="null"/>.</returns>
    public SharpSqlEntity FetchEntityByPrimaryKey<EntityType>(object primaryKey, Expression<Func<EntityType, object>> joins)
        where EntityType : SharpSqlEntity
    {
        PrimaryKey.Keys[0].Value = primaryKey;

        return FetchEntity(PrimaryKey, joins);
    }

    /// <summary>
    /// Fetches an <see cref="SharpSqlEntity"/> based on the provided combined primary key.
    /// </summary>
    /// <param name="primaryKeys">The combined primary key.</param>
    /// <returns>Returns the fetched <see cref="SharpSqlEntity"/> or <see langword="null"/>.</returns>
    public SharpSqlEntity FetchEntityByPrimaryKey(params object[] primaryKeys)
    {
        for (int i = 0; i < primaryKeys.Length; i++)
        {
            PrimaryKey.Keys[i].Value = primaryKeys[i];
        }

        return FetchEntity(PrimaryKey);
    }

    /// <summary>
    /// Fetches an <see cref="SharpSqlEntity"/> based on the provided combined primary key and join(s).
    /// </summary>
    /// <typeparam name="EntityType"></typeparam>
    /// <param name="joins">The fields you want to join.</param>
    /// <param name="primaryKeys">The combined primary key.</param>
    /// <returns>Returns the fetched <see cref="SharpSqlEntity"/> or <see langword="null"/>.</returns>
    public SharpSqlEntity FetchEntityByPrimaryKey<EntityType>(Expression<Func<EntityType, object>> joins, params object[] primaryKeys)
        where EntityType : SharpSqlEntity
    {
        for (int i = 0; i < primaryKeys.Length; i++)
        {
            PrimaryKey.Keys[i].Value = primaryKeys[i];
        }

        return FetchEntity(PrimaryKey, joins);
    }

    /// <summary>
    /// Returns a value indicating whether <see langword="this"/> instance is equal to a specified <see cref="SharpSqlEntity"/>.
    /// </summary>
    /// <param name="other">A <see cref="SharpSqlEntity"/> value to compare to <see langword="this"/> instance.</param>
    /// <returns><see langword="true"/> if 'other' has the same value as <see langword="this"/> instance; otherwise, <see langword="false"/>.</returns>
    public bool Equals(SharpSqlEntity other)
    {
        if (other is null)
        {
            return false;
        }
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        if (GetType() != other.GetType())
        {
            return false;
        }

        return GetHashCode().Equals(other.GetHashCode());
    }

    /// <summary>
    /// Returns the current <see cref="SharpSqlEntity"/> to its provided runtime type.
    /// </summary>
    /// <typeparam name="RuntimeType"></typeparam>
    /// <returns>Returns the runtime type.</returns>
    public RuntimeType ValueAs<RuntimeType>() where RuntimeType : SharpSqlEntity
    {
        if (this is RuntimeType entity)
        {
            return entity;
        }

        throw new InvalidCastException($"Cannot convert object of type [{GetType().Name}] to type [{typeof(RuntimeType).Name}].");
    }

    /// <summary>
    /// Returns if the specified fields is dirty.
    /// </summary>
    /// <exception cref="KeyNotFoundException"></exception>
    /// <param name="field"></param>
    /// <returns></returns>
    public bool IsFieldDirty(string field)
    {
        return DirtyTracker.IsDirty(field);
    }

    public void MarkFieldsAsDirty(params string[] fields)
    {
        MarkDirtyTrackerFieldsAs(true, fields);
    }

    internal void MarkDirtyTrackerFieldsAs(bool isDirty, params string[] fields)
    {
        foreach (var field in fields)
        {
            if (this[field] is SharpSqlEntity join)
            {
                throw new InvalidOperationException($"You can't mark the {GetType()}.{join.GetType().Name} as dirty, you have to mark the fields in {GetType()}.{join.GetType().Name} as dirty.");
            }
            else if (ObjectState == ObjectState.ExternalRecord || DisableChangeTracking)
            {
                DirtyTracker.Update(field, isDirty);
            }
            else
                throw new InvalidOperationException("ObjectState must be ExternalRecord or DisableChangeTracking must be set to true to mark fields as dirty. When the ObjectState is New, fields do not have to be marked as dirty.");
        }
    }

    /// <summary>
    /// The INNER JOIN keyword selects records that have matching values in both tables.
    /// </summary>
    public SharpSqlEntity Inner() => default;

    /// <summary>
    /// The LEFT JOIN keyword returns all records from the left table, and the matched records
    /// from the right table. The result is NULL from the right side, if there is no match.
    /// </summary>
    public SharpSqlEntity Left() => default;

    internal SharpSqlEntity ShallowCopy(ObjectState objectState = ObjectState.OriginalFetchedValue)
    {
        var copy = MemberwiseClone() as SharpSqlEntity;

        copy.Relations = new List<SharpSqlEntity>(Relations);
        copy.ObjectState = objectState;

        return copy;
    }

    internal SharpSqlEntity CloneToChild(SharpSqlEntity child)
    {
        child.DirtyTracker = DirtyTracker;
        child.DisableChangeTracking = DisableChangeTracking;
        child.ExecutedQuery = ExecutedQuery;
        child.MutableTableScheme = MutableTableScheme;
        child.ObjectState = ObjectState;
        child.PrimaryKey = PrimaryKey;
        child.Relations = Relations;

        return child;
    }

    internal PropertyInfo[] GetPrimaryKeyPropertyInfo()
    {
        PropertyInfo[] propertyInfo = new PropertyInfo[PrimaryKey.Count];

        for (int i = 0; i < PrimaryKey.Count; i++)
        {
            propertyInfo[i] = GetType().GetProperty(PrimaryKey.Keys[i].PropertyName);

            if (propertyInfo[i] == null)
            {
                throw new ArgumentException($"No PK-property found for name: [{PrimaryKey.Keys[i]}] in [{GetType().Name}].");
            }
        }

        return propertyInfo;
    }

    private SharpSqlEntity FetchEntity(SharpSqlPrimaryKey primaryKey, Expression joinExpression = null)
    {
        BinaryExpression whereExpression = null;

        for (int i = 0; i < PrimaryKey.Count; i++)
        {
            if (primaryKey.Keys[i].Value == DBNull.Value)
            {
                // DBNull, so there's nothing to fetch.
                return null;
            }

            // Contains the id represented as a MemberExpression: {x.InternalPrimaryKeyName}.
            var memberExpression = Expression.Property(Expression.Parameter(GetType(), "x"), GetPrimaryKeyPropertyInfo()[i]);

            // Contains the actual id represented as a ConstantExpression: {id_value}.
            var constantExpression = Expression.Constant(primaryKey.Keys[i].Value, primaryKey.Keys[i].Value.GetType());

            // Combines the expressions represtend as a Expression: {(x.InternalPrimaryKeyName == id_value)}
            if (whereExpression == null)
                whereExpression = Expression.Equal(memberExpression, constantExpression);
            else
                whereExpression = Expression.AndAlso(whereExpression, Expression.Equal(memberExpression, constantExpression));
        }

        // Instantiates and fetches the run-time collection.
        var collection = Activator.CreateInstance(SharpSqlCache.CollectionEntityRelations[GetType()]);

        // Sets the InternalWhere with the WhereExpression.
        collection.GetType().GetMethod(nameof(SharpSqlCollection<SharpSqlEntity>.InternalWhere), NonPublicFlags, null, new Type[] { typeof(BinaryExpression) }, null).Invoke(collection, new object[] { whereExpression });

        if (ObjectState != ObjectState.Record)
        {
            // Fetches the data.
            collection.GetType().GetMethod(nameof(SharpSqlCollection<SharpSqlEntity>.Fetch), NonPublicFlags, null, new Type[] { typeof(SharpSqlEntity), typeof(long), typeof(Expression) }, null).Invoke(collection, new object[] { this, joinExpression == null ? 1 : -1, joinExpression });

            if (!UnitTestUtilities.IsUnitTesting && IsNew)
                return null;
            
            ObjectState = ObjectState.Fetched;

            ExecutedQuery = (string)collection.GetType().GetProperty(nameof(SharpSqlCollection<SharpSqlEntity>.ExecutedQuery)).GetValue(collection);

            if (OriginalFetchedValue != null)
            {
                OriginalFetchedValue.ExecutedQuery = ExecutedQuery;
            }
        }

        return this;
    }

    public SharpSqlEntity FetchUsingUC(string columnName, string value)
    {
        if (columnName == null)
            throw new ArgumentNullException(nameof(columnName));
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        if (!SharpSqlCache.UniqueConstraints.Contains((GetType(), columnName)))
            throw new IllegalUniqueConstraintException(columnName);

        // Contains the UC represented as a MemberExpression: {x.columnName}.
        var memberExpression = Expression.Property(Expression.Parameter(GetType(), "x"), GetType().GetProperty(columnName, PublicIgnoreCaseFlags | NonPublicFlags));

        // Contains the actual UC represented as a ConstantExpression: {value}.
        var constantExpression = Expression.Constant(value, value.GetType());

        // Combines the expressions represtend as a Expression: {(x.columnName == value)}
        var whereExpression = Expression.Equal(memberExpression, constantExpression);

        // Instantiates and fetches the run-time collection.
        var collection = Activator.CreateInstance(SharpSqlCache.CollectionEntityRelations[GetType()]);

        // Sets the InternalWhere with the WhereExpression.
        collection.GetType().GetMethod(nameof(SharpSqlCollection<SharpSqlEntity>.InternalWhere), NonPublicFlags, null, new Type[] { typeof(BinaryExpression) }, null).Invoke(collection, new object[] { whereExpression });

        // Fetches the data.
        collection.GetType().GetMethod(nameof(SharpSqlCollection<SharpSqlEntity>.Fetch), NonPublicFlags, null, new Type[] { typeof(SharpSqlEntity), typeof(long), typeof(Expression) }, null).Invoke(collection, new object[] { this, 1, null });

        // When nothing is found, it returns an empty and therefore 'new' object.
        if (IsNew)
            return null;

        ObjectState = ObjectState.Fetched;

        if (!UnitTestUtilities.IsUnitTesting && IsNew)
            return null;

        ExecutedQuery = (string)collection.GetType().GetProperty(nameof(SharpSqlCollection<SharpSqlEntity>.ExecutedQuery)).GetValue(collection);

        if (OriginalFetchedValue != null)
        {
            OriginalFetchedValue.ExecutedQuery = ExecutedQuery;
        }

        return this;
    }

    private void UpdateIsDirtyList()
    {
        // @Todo: needs a light and improved version for certain cases.

        for (int i = 0; i < MutableTableScheme.Count; i++)
        {
            // When the mutable field is a join that's new, while the parent has change tracking disabled
            // it should not be skipped.
            bool hasJoinThatsNew = (this[MutableTableScheme[i]] as SharpSqlEntity)?.IsNew ?? false;

            if (IsNew                                                                           // When an object is new everything is 'dirty' by default.
             || DisableChangeTracking && !hasJoinThatsNew                                       // When changetracking is disabeld, dirty fields have to be set manually.
             || (ObjectState == ObjectState.ExternalRecord && OriginalFetchedValue == null))    // Because the source is exteral there is no original fetched value. Therefore the dirty fields have to be set manually
            {
                continue;
            }

            var thisValue = this[MutableTableScheme[i]];
            var originalValue = OriginalFetchedValue?[MutableTableScheme[i]];

            if (Relations.Any(x => x != null && x.GetType().Name == MutableTableScheme[i]) && (thisValue == null || this[MutableTableScheme[i]].GetType() != GetType()))
            {
                if (thisValue != null && !thisValue.Equals(originalValue))
                {
                    DirtyTracker.Update(MutableTableScheme[i], true);
                }
                else
                {
                    DirtyTracker.Update(MutableTableScheme[i], (thisValue as SharpSqlEntity)?.IsDirty ?? false);
                }
            }
            else
            {
                if ((thisValue != null && !thisValue.Equals(originalValue))
                 || (thisValue == null && originalValue != null))
                {
                    DirtyTracker.Update(MutableTableScheme[i], true);
                }
                else
                {
                    DirtyTracker.Update(MutableTableScheme[i], false);
                }
            }
        }
    }

    private void UpdateSinglePrimaryKey(object id)
    {
        this[PrimaryKey.Keys[0].ColumnName] = id;
        PrimaryKey.Keys[0].Value = id;
    }

    private void UpdateCombinedPrimaryKey()
    {
        for (int i = 0; i < PrimaryKey.Keys.Count; i++)
        {
            PrimaryKey.Keys[i].Value = this[PrimaryKey.Keys[i].ColumnName];
        }
    }
}