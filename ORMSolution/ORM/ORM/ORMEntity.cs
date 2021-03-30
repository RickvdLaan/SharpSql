using Newtonsoft.Json;
using ORM.Attributes;
using ORM.Exceptions;
using ORM.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ORMNUnit")]

namespace ORM
{
    public class ORMEntity : ORMObject, IEquatable<ORMEntity>, IORMEntity
    {
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

        /// <summary>
        /// Gets whether the <see cref="ORMEntity"/> has an auto-increment primary key field.
        /// </summary>
        [JsonIgnore]
        public bool IsAutoIncrement { get { return PrimaryKey.Keys.Any(key => key.IsAutoIncrement); } }

        /// <summary>
        /// Gets whether the <see cref="ORMEntity"/> is new or not.
        /// </summary>
        public bool IsNew { get; internal set; } = true;

        /// <summary>
        /// Gets whether change tracking is enabled or disabled, it's <see langword="false"/> by <see langword="default"/> and
        /// can be set to <see langword="true"/> through the <see cref="ORMEntity"/> constructor.
        /// </summary>
        [JsonIgnore]
        public bool DisableChangeTracking { get; internal set; } = false;

        /// <summary>
        /// Gets the <see cref="ORMPrimaryKey"/> of the current <see cref="ORMEntity"/>.
        /// </summary>
        [JsonIgnore]
        public ORMPrimaryKey PrimaryKey { get; internal set; } = null;

        /// <summary>
        /// Gets whether the value of a <see cref="ORMEntity"/> has changed.
        /// </summary>
        public bool IsDirty
        {
            get
            {
                if (!IsNew && !DisableChangeTracking && OriginalFetchedValue == null)
                    return false;

                UpdateIsDirtyList();

                return IsDirtyList.Any(x => x.IsDirty == true);
            }
        }

        /// <summary>
        /// Gets the table scheme from the current <see cref="ORMEntity"/>.
        /// </summary>
        [JsonIgnore]
        public ReadOnlyCollection<string> TableScheme { get { return ORMUtilities.CachedColumns[GetType()].AsReadOnly(); } }

        internal bool IsMarkAsDeleted { get; set; } = false;

        internal List<ORMEntity> EntityRelations { get; private set; } = new List<ORMEntity>();

        internal (string ColumnName, bool IsDirty)[] IsDirtyList { get; set; } = null;

        internal ORMEntity OriginalFetchedValue { get; set; } = null;

        internal List<string> MutableTableScheme { get; private set; } = null;

        /// <summary>
        /// Initializes a new instance of <see cref="ORMEntity"/>.
        /// </summary>
        public ORMEntity()
        {
            InitializePrimaryKeys();
            InitializeMutableTableSchema();

            IsNew = OriginalFetchedValue == null;

            if (!DisableChangeTracking)
            {
                IsDirtyList = new (string, bool)[TableScheme.Count - PrimaryKey.Keys.Count(key => key.IsAutoIncrement)];
            }
        }

        private void InitializePrimaryKeys()
        {
            var attributes = new List<ORMPrimaryKeyAttribute>();

            foreach (var property in GetType().GetProperties())
            {
                foreach (ORMPrimaryKeyAttribute attribute in property.GetCustomAttributes(typeof(ORMPrimaryKeyAttribute), true))
                {
                    attribute.PropertyName = property.Name;
                    attribute.ColumnName = (property.GetCustomAttributes(typeof(ORMColumnAttribute), true).FirstOrDefault() as ORMColumnAttribute)?.ColumnName ?? property.Name;
                    attributes.Add(attribute);
                }
            }

            PrimaryKey = new ORMPrimaryKey(attributes.Count);

            if (attributes.Count > 0)
            {
                foreach (var attribute in attributes)
                {
                    PrimaryKey.Add(attribute.PropertyName, attribute.ColumnName, null, attribute.IsAutoIncrement);
                }
            }
            else
            {
                throw new ORMPrimaryKeyAttributeNotImplementedException(GetType());
            }
        }

        private void InitializeMutableTableSchema()
        {
            if (ORMUtilities.CachedMutableColumns.ContainsKey(GetType()))
            {
                MutableTableScheme = ORMUtilities.CachedMutableColumns[GetType()];
                return;
            }

            MutableTableScheme = new List<string>(TableScheme.Count - PrimaryKey.Keys.Where(pk => pk.IsAutoIncrement).Count());

            foreach (var columnName in TableScheme)
            {
                if (PrimaryKey.Keys.Any(pk => pk.ColumnName == columnName && pk.IsAutoIncrement))
                    continue;

                MutableTableScheme.Add(columnName);
            }

            ORMUtilities.CachedMutableColumns[GetType()] = MutableTableScheme;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ORMEntity"/>.
        /// </summary>
        /// <param name="disableChangeTracking">Enables or disables change tracking for the current entity.</param>
        public ORMEntity(bool disableChangeTracking = false) : this()
        {
            DisableChangeTracking = disableChangeTracking;
        }

        /// <summary>
        /// Gets or sets the accessors of the current <see cref="ORMEntity"/>.
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
                        var columnAttribute = property.GetCustomAttributes(typeof(ORMColumnAttribute), false).FirstOrDefault() as ORMColumnAttribute;

                        if (columnAttribute?.ColumnName == columnName)
                        {
                            return GetType().GetProperty(property.Name, PublicIgnoreCaseFlags | NonPublicFlags).GetValue(this);
                        }
                    }
                }

                return propertyInfo.GetValue(this);
            }
            set
            {
                var propertyInfo = GetType().GetProperty(columnName);

                if (propertyInfo == null)
                {
                    foreach (var property in GetType().GetProperties())
                    {
                        var columnAttribute = property.GetCustomAttributes(typeof(ORMColumnAttribute), false).FirstOrDefault() as ORMColumnAttribute;

                        if (columnAttribute?.ColumnName == columnName)
                        {
                            GetType().GetProperty(property.Name, PublicIgnoreCaseFlags | NonPublicFlags).SetValue(this, value);

                            return;
                        }
                    }

                    throw new NotImplementedException($"The property [{columnName}] was not found in entity [{GetType().Name}].");
                }

                propertyInfo.SetValue(this, value);
            }
        }

        /// <summary>
        /// Returns a value indicating whether <see langword="this"/> instance is equal to a specified <see langword="object"/>.
        /// </summary>
        /// <param name="other">A <see langword="object"/> value to compare to <see langword="this"/> instance.</param>
        /// <returns><see langword="true"/> if 'other' has the same value as <see langword="this"/> instance; otherwise, <see langword="false"/>.</returns>
        public override bool Equals(object other)
        {
            return Equals(other as ORMEntity);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A hash code for the current <see cref="ORMEntity"/></returns>
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
        /// Indicates whether two <see cref="ORMEntity"/> objects are equal.
        /// </summary>
        /// <param name="leftSide">The first <see cref="ORMEntity"/> to compare.</param>
        /// <param name="rightSide">The second <see cref="ORMEntity"/> to compare.</param>
        /// <returns><see langword="true"/> if left is equal to right; otherwise, <see langword="false"/>.</returns>
        public static bool operator ==(ORMEntity leftSide, ORMEntity rightSide)
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
        ///  Indicates whether two <see cref="ORMEntity"/> objects are not equal.
        /// </summary>
        /// <param name="leftSide">The first <see cref="ORMEntity"/> to compare.</param>
        /// <param name="rightSide">The second <see cref="ORMEntity"/> to compare.</param>
        /// <returns><see langword="true"/> if left is not equal to right; otherwise, <see langword="false"/>.</returns>
        public static bool operator !=(ORMEntity leftSide, ORMEntity rightSide)
        {
            return !(leftSide == rightSide);
        }

        /// <summary>
        /// Saves the current <see cref="ORMEntity"/> (changes) to the database.
        /// </summary>
        public virtual void Save()
        {
            if (IsDirty)
            {
                var sqlBuilder = new SQLBuilder();

                // @Perfomance: it fixes some issues, but this is terrible for the performance.
                // Needs to be looked at! -Rick, 12 December 2020
                foreach (var column in MutableTableScheme)
                {
                    // When a subEntity is not filled through the parent the PopulateChildEntity method
                    // isn't called and therefore the subEntity is not added to the EntityRelations.
                    if (this[column] != null && this[column].GetType().IsSubclassOf(typeof(ORMEntity)))
                    {
                        var subEntity = Activator.CreateInstance(this[column].GetType().UnderlyingSystemType) as ORMEntity;

                        if (!EntityRelations.Any(x => x.GetType() == subEntity.GetType()))
                        {
                            if ((this[column] as ORMEntity).IsDirty 
                             || (OriginalFetchedValue?[column] == null && !(this[column] as ORMEntity).IsDirty))
                            {
                                EntityRelations.Add(subEntity);
                            }
                        }
                    }
                }

                foreach (var relation in EntityRelations)
                {
                    if (this[relation.GetType().Name] == null && OriginalFetchedValue[relation.GetType().Name] != null)
                    {
                        continue;
                    }
                    else if ((this[relation.GetType().Name] as ORMEntity).IsDirty)
                    {
                        (this[relation.GetType().Name] as ORMEntity).Save();

                        for (int i = 0; i < relation.PrimaryKey.Count; i++)
                        {
                            var entityRelationId = (int)(this[relation.GetType().Name] as ORMEntity)[relation.PrimaryKey.Keys[i].ColumnName];
                            var entityJoin = this[relation.GetType().Name];

                            entityJoin.GetType().GetProperty(relation.PrimaryKey.Keys[i].ColumnName).SetValue(entityJoin, entityRelationId);
                            entityJoin.GetType().GetProperty(nameof(ExecutedQuery)).SetValue(entityJoin, (this[relation.GetType().Name] as ORMEntity).ExecutedQuery);
                        }
                    }
                }

                if (IsNew || IsNew && EntityRelations.Any(r => r.IsNew))
                {
                    sqlBuilder.BuildNonQuery(this, NonQueryType.Insert);

                    int id = SQLExecuter.ExecuteNonQuery(sqlBuilder, NonQueryType.Insert);

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
                    sqlBuilder.BuildNonQuery(this, NonQueryType.Update);
                    SQLExecuter.ExecuteNonQuery(sqlBuilder, NonQueryType.Update);
                }

                ExecutedQuery = sqlBuilder.GeneratedQuery;
            }
        }

        /// <summary>
        /// Deletes the current <see cref="ORMEntity"/> from the database.
        /// </summary>
        public virtual void Delete()
        {
            if (!IsNew)
            {
                var sqlBuilder = new SQLBuilder();
                sqlBuilder.BuildNonQuery(this, NonQueryType.Delete);
                SQLExecuter.ExecuteNonQuery(sqlBuilder, NonQueryType.Delete);
                IsMarkAsDeleted = true;
                // @Important:
                // Do we need a ORMEntityState enum?
                // We need to mark the object as deleted, or it has to be marked as new again.
                // Something has to be done here, has to be thought out.
                // -Rick, 25 September 2020
            }
        }

        /// <summary>
        /// Fetches an <see cref="ORMEntity"/> based on the provided primary key.
        /// </summary>
        /// <param name="primaryKey">The primary key.</param>
        /// <returns>Returns the fetched <see cref="ORMEntity"/> or <see langword="null"/>.</returns>
        public ORMEntity FetchEntityByPrimaryKey(object primaryKey)
        {
            PrimaryKey.Keys[0].Value = primaryKey;

            return FetchEntity(PrimaryKey);
        }

        /// <summary>
        /// Fetches an <see cref="ORMEntity"/> based on the provided primary key and join(s).
        /// </summary>
        /// <typeparam name="EntityType"></typeparam>
        /// <param name="primaryKey">The primary key.</param>
        /// <param name="joins">The fields you want to join.</param>
        /// <returns>Returns the fetched <see cref="ORMEntity"/> or <see langword="null"/>.</returns>
        public ORMEntity FetchEntityByPrimaryKey<EntityType>(object primaryKey, Expression<Func<EntityType, object>> joins)
            where EntityType : ORMEntity
        {
            PrimaryKey.Keys[0].Value = primaryKey;

            return FetchEntity(PrimaryKey, joins);
        }

        /// <summary>
        /// Fetches an <see cref="ORMEntity"/> based on the provided combined primary key.
        /// </summary>
        /// <param name="primaryKeys">The combined primary key.</param>
        /// <returns>Returns the fetched <see cref="ORMEntity"/> or <see langword="null"/>.</returns>
        public ORMEntity FetchEntityByPrimaryKey(params object[] primaryKeys)
        {
            for (int i = 0; i < primaryKeys.Length; i++)
            {
                PrimaryKey.Keys[i].Value = primaryKeys[i];
            }

            return FetchEntity(PrimaryKey);
        }

        /// <summary>
        /// Fetches an <see cref="ORMEntity"/> based on the provided combined primary key and join(s).
        /// </summary>
        /// <typeparam name="EntityType"></typeparam>
        /// <param name="joins">The fields you want to join.</param>
        /// <param name="primaryKeys">The combined primary key.</param>
        /// <returns>Returns the fetched <see cref="ORMEntity"/> or <see langword="null"/>.</returns>
        public ORMEntity FetchEntityByPrimaryKey<EntityType>(Expression<Func<EntityType, object>> joins, params object[] primaryKeys)
            where EntityType : ORMEntity
        {
            for (int i = 0; i < primaryKeys.Length; i++)
            {
                PrimaryKey.Keys[i].Value = primaryKeys[i];
            }

            return FetchEntity(PrimaryKey, joins);
        }

        /// <summary>
        /// Returns a value indicating whether <see langword="this"/> instance is equal to a specified <see cref="ORMEntity"/>.
        /// </summary>
        /// <param name="other">A <see cref="ORMEntity"/> value to compare to <see langword="this"/> instance.</param>
        /// <returns><see langword="true"/> if 'other' has the same value as <see langword="this"/> instance; otherwise, <see langword="false"/>.</returns>
        public bool Equals(ORMEntity other)
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
        /// Returns the current <see cref="ORMEntity"/> to its provided runtime type.
        /// </summary>
        /// <typeparam name="RuntimeType"></typeparam>
        /// <returns>Returns the runtime type.</returns>
        public RuntimeType ValueAs<RuntimeType>() where RuntimeType : ORMEntity
        {
            if (this is RuntimeType entity)
            {
                return entity;
            }

            throw new InvalidCastException($"Cannot convert object of type [{GetType().Name}] to type [{typeof(RuntimeType).Name}].");
        }

        /// <summary>
        /// The INNER JOIN keyword selects records that have matching values in both tables.
        /// </summary>
        public ORMEntity Inner() => default;

        /// <summary>
        /// The LEFT JOIN keyword returns all records from the left table, and the matched records
        /// from the right table. The result is NULL from the right side, if there is no match.
        /// </summary>
        public ORMEntity Left() => default;

        internal ORMEntity ShallowCopy()
        {
            var copy = MemberwiseClone() as ORMEntity;

            copy.EntityRelations = new List<ORMEntity>(EntityRelations);

            return copy;
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

        private ORMEntity FetchEntity(ORMPrimaryKey primaryKey, Expression joinExpression = null)
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
                var memberExpression = Expression.Property(Expression.Parameter(GetType(), $"x"), GetPrimaryKeyPropertyInfo()[i]);

                // Contains the actual id represented as a ConstantExpression: {id_value}.
                var constantExpression = Expression.Constant(primaryKey.Keys[i].Value, primaryKey.Keys[i].Value.GetType());

                // Combines the expressions represtend as a Expression: {(x.InternalPrimaryKeyName == id_value)}
                if (whereExpression == null)
                    whereExpression = Expression.Equal(memberExpression, constantExpression);
                else
                    whereExpression = Expression.AndAlso(whereExpression, Expression.Equal(memberExpression, constantExpression));
            }

            // Instantiates and fetches the run-time collection.
            var collection = Activator.CreateInstance(ORMUtilities.CollectionEntityRelations[GetType()]);

            // Sets the InternalWhere with the WhereExpression.
            collection.GetType().GetMethod(nameof(ORMCollection<ORMEntity>.InternalWhere), NonPublicFlags, null, new Type[] { typeof(BinaryExpression) }, null).Invoke(collection, new object[] { whereExpression });
            
            // Fetches the data.
            collection.GetType().GetMethod(nameof(ORMCollection<ORMEntity>.Fetch), NonPublicFlags, null, new Type[] { typeof(ORMEntity), typeof(long), typeof(Expression) }, null).Invoke(collection, new object[] { this, joinExpression == null ? 1 : -1, joinExpression });

            if (!UnitTestUtilities.IsUnitTesting && IsNew)
                return null;

            ExecutedQuery = (string)collection.GetType().GetProperty(nameof(ORMCollection<ORMEntity>.ExecutedQuery)).GetValue(collection);

            if (OriginalFetchedValue != null)
            {
                OriginalFetchedValue.ExecutedQuery = ExecutedQuery;
            }

            return this;
        }

        public ORMEntity FetchUsingUC(string columnName, string value)
        {
            if(columnName == null)
                throw new ArgumentNullException($"Parameter [{ nameof(columnName) }] cannot be null.");
            if (value == null)
                throw new ArgumentNullException($"Parameter [{ nameof(value) }] cannot be null.");
            if (!ORMUtilities.UniqueConstraints.Contains((GetType(), columnName)))
                throw new ORMIllegalUniqueConstraintException(columnName);

            // Contains the UC represented as a MemberExpression: {x.columnName}.
            var memberExpression = Expression.Property(Expression.Parameter(GetType(), $"x"), GetType().GetProperty(columnName, PublicIgnoreCaseFlags | NonPublicFlags));

            // Contains the actual UC represented as a ConstantExpression: {value}.
            var constantExpression = Expression.Constant(value, value.GetType());

            // Combines the expressions represtend as a Expression: {(x.columnName == value)}
            var whereExpression = Expression.Equal(memberExpression, constantExpression);

            // Instantiates and fetches the run-time collection.
            var collection = Activator.CreateInstance(ORMUtilities.CollectionEntityRelations[GetType()]);

            // Sets the InternalWhere with the WhereExpression.
            collection.GetType().GetMethod(nameof(ORMCollection<ORMEntity>.InternalWhere), NonPublicFlags, null, new Type[] { typeof(BinaryExpression) }, null).Invoke(collection, new object[] { whereExpression });

            // Fetches the data.
            collection.GetType().GetMethod(nameof(ORMCollection<ORMEntity>.Fetch), NonPublicFlags, null, new Type[] { typeof(ORMEntity), typeof(long), typeof(Expression) }, null).Invoke(collection, new object[] { this, 1, null });

            if (!UnitTestUtilities.IsUnitTesting && IsNew)
                return null;

            ExecutedQuery = (string)collection.GetType().GetProperty(nameof(ORMCollection<ORMEntity>.ExecutedQuery)).GetValue(collection);

            if (OriginalFetchedValue != null)
            {
                OriginalFetchedValue.ExecutedQuery = ExecutedQuery;
            }

            return this;
        }

        private void UpdateIsDirtyList()
        {
            for (int i = 0; i < MutableTableScheme.Count; i++)
            {
                // When an object is new, or change tracking is disabled everything is 'dirty' by default.
                if (IsNew || DisableChangeTracking)
                {
                    IsDirtyList[i] = (MutableTableScheme[i], true);
                    continue;
                }

                var thisValue = this[MutableTableScheme[i]];
                var originalValue = OriginalFetchedValue?[MutableTableScheme[i]];

                if (EntityRelations.Any(x => x != null && x.GetType().Name == MutableTableScheme[i]) && (thisValue == null || this[MutableTableScheme[i]].GetType() != GetType()))
                {
                    if (thisValue != null && !thisValue.Equals(originalValue))
                    {
                        IsDirtyList[i] = (MutableTableScheme[i], true);
                    }
                    else
                    {
                        IsDirtyList[i] = (MutableTableScheme[i], (thisValue as ORMEntity)?.IsDirty ?? false);
                    }
                }
                else
                {
                    if ((thisValue != null && !thisValue.Equals(originalValue))
                     || (thisValue == null && originalValue != null))
                    {
                        IsDirtyList[i] = (MutableTableScheme[i], true);
                    }
                    else
                    {
                        IsDirtyList[i] = (MutableTableScheme[i], false);
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
}