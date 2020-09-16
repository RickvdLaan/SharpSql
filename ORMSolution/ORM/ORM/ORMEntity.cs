﻿using ORM.Attributes;
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
        public string ExecutedQuery { get; internal set; } = "An unknown query has been executed.";

        public bool IsNew { get; internal set; } = true;

        public bool IsDirty
        {
            get
            {
                if (DisableChangeTracking || IsNew)
                    return true;

                if (!IsNew && OriginalFetchedValue == null)
                    return false;

                UpdateIsDirtyList();

                return IsDirtyList.Any(x => x.IsDirty == true);
            }
        }

        public ReadOnlyCollection<string> TableScheme
        {
            get
            {
                if (MutableTableScheme == null)
                    MutableTableScheme = ORMUtilities.CachedColumns[GetType()];

                return MutableTableScheme.AsReadOnly();
            }
        }

        public bool DisableChangeTracking { get; internal set; }

        public ORMPrimaryKey PrimaryKey { get; internal set; }

        internal ORMEntity OriginalFetchedValue { get; set; } = null;

        internal List<ORMEntity> EntityRelations = new List<ORMEntity>();

        internal List<string> MutableTableScheme { get; set; }

        internal (string ColumnName, bool IsDirty)[] IsDirtyList { get; set; }

        private void UpdateIsDirtyList()
        {
            for (int i = 0; i < MutableTableScheme.Count; i++)
            {
                if (PrimaryKey.Keys.Any(x => x.ColumnName == MutableTableScheme[i])) // ToDo: && !AutoIncrement + other locations.
                    continue;

                var thisValue = this[MutableTableScheme[i]];
                var originalValue = OriginalFetchedValue?[MutableTableScheme[i]];

                if (EntityRelations.Any(x => x != null && x.GetType().Name == MutableTableScheme[i]) && (thisValue == null || this[MutableTableScheme[i]].GetType() != GetType()))
                {
                    if (thisValue != null && !thisValue.Equals(originalValue))
                    {
                        IsDirtyList[i - 1] = (MutableTableScheme[i], true);
                    }
                    else
                    {
                        IsDirtyList[i - 1] = (MutableTableScheme[i], (thisValue as ORMEntity)?.IsDirty ?? false);
                    }
                }
                else
                {
                    if ((thisValue != null && !thisValue.Equals(originalValue))
                     || (thisValue == null && originalValue != null))
                    {
                        IsDirtyList[i - 1] = (MutableTableScheme[i], true);
                    }
                    else
                    {
                        IsDirtyList[i - 1] = (MutableTableScheme[i], false);
                    }
                }
            }
        }

        public ORMEntity()
        {
            var attributes = new List<ORMPrimaryKeyAttribute>();

            foreach (var property in GetType().GetProperties())
            {
                foreach (ORMPrimaryKeyAttribute attribute in property.GetCustomAttributes(typeof(ORMPrimaryKeyAttribute), true))
                {
                    attribute.Name = property.Name;
                    attributes.Add(attribute);
                }
            }

            PrimaryKey = new ORMPrimaryKey(attributes.Count);

            if (attributes.Count > 1)
            {
                foreach (var attribute in attributes)
                {
                    PrimaryKey.Add(attribute.Name, null);
                }
            }
            else if (attributes.Count == 1)
            {
                PrimaryKey.Add(attributes[0].Name, null);
            }
            else
            {
                throw new ORMPrimaryKeyAttributeNotImplementedException(GetType());
            }

            IsNew = OriginalFetchedValue == null;

            if (!DisableChangeTracking)
            {
                IsDirtyList = new (string, bool)[TableScheme.Count - PrimaryKey.Count];
            }
        }

        public ORMEntity(bool disableChangeTracking = false) : this()
        {
            DisableChangeTracking = disableChangeTracking;
        }

        public object this[string columnName]
        {
            get { return GetType().GetProperty(columnName, PublicIgnoreCaseFlags | NonPublicFlags).GetValue(this); }
            set  { GetType().GetProperty(columnName).SetValue(this, value); }
        }

        public override bool Equals(object other)
        {
            return Equals(other as ORMEntity);
        }

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

        public override int GetHashCode()
        {
            // Overflow is fine in this case.
            unchecked
            {
                var hash = (int)2166136261;

                for (int i = 0; i < MutableTableScheme.Count; i++)
                {
                    hash = (hash * 16777619) ^ this[MutableTableScheme[i]].GetHashCode();
                }

                hash = (hash * 16777619) ^ IsDirty.GetHashCode();
                hash = (hash * 16777619) ^ PrimaryKey.GetHashCode();

                return hash;
            }
        }

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

        public static bool operator !=(ORMEntity leftSide, ORMEntity rightSide)
        {
            return !(leftSide == rightSide);
        }

        internal PropertyInfo[] GetPrimaryKeyPropertyInfo()
        {
            PropertyInfo[] propertyInfo = new PropertyInfo[PrimaryKey.Count];

            for (int i = 0; i < PrimaryKey.Count; i++)
            {
                propertyInfo[i] = GetType().GetProperty(PrimaryKey.Keys[i].ColumnName);

                if (propertyInfo[i] == null)
                {
                    throw new ArgumentException($"No PK-property found for name: [{PrimaryKey.Keys[i]}] in [{GetType().Name}].");
                }
            }

            return propertyInfo;
        }

        public ORMEntity FetchEntityByPrimaryKey(object primaryKey)
        {
            PrimaryKey.Keys[0].Value = primaryKey;

            return FetchDynamicEntity(PrimaryKey);
        }

        public ORMEntity FetchEntityByPrimaryKey(params object[] primaryKeys)
        {
            for (int i = 0; i < primaryKeys.Length; i++)
            {
                PrimaryKey.Keys[i].Value = primaryKeys[i];
            }

            return FetchDynamicEntity(PrimaryKey);
        }

        private ORMEntity FetchDynamicEntity(ORMPrimaryKey primaryKey)
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
            collection.GetType().GetMethod(nameof(ORMCollection<ORMEntity>.InternalWhere), NonPublicFlags, null, new Type[] { typeof(BinaryExpression) }, null).Invoke(collection, new object[] { whereExpression });
            collection.GetType().GetMethod(nameof(ORMCollection<ORMEntity>.Fetch), NonPublicFlags, null, new Type[] { typeof(ORMEntity), typeof(long) }, null).Invoke(collection, new object[] { this, 1 });

            // Old - Pt.1 - undecided on which is better at this point.
            //dynamic collection = Activator.CreateInstance(ORMUtilities.CollectionEntityRelations[GetType()]);
            //collection.InternalWhere(whereExpression);
            //collection.Fetch(this, 1);

            if (!ORMUtilities.IsUnitTesting && IsNew)
                throw new Exception($"No [{GetType().Name}] found for {string.Join(", ", PrimaryKey.Keys.Select(x => x.ToString()).ToArray())}.");

            ExecutedQuery = (string)collection.GetType().GetProperty(nameof(ORMCollection<ORMEntity>.ExecutedQuery)).GetValue(collection);

            // Old - Pt.2 - undecided on which is better at this point.
            // ExecutedQuery = collection.ExecutedQuery;

            if (OriginalFetchedValue != null)
            {
                OriginalFetchedValue.ExecutedQuery = ExecutedQuery;
            }

            return this;
        }

        public virtual void Save()
        {
            if (IsDirty)
            {
                var sqlBuilder = new SQLBuilder();

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

                if (IsNew && EntityRelations.Count == 0 || IsNew && EntityRelations.Any(r => r.IsNew))
                {
                    sqlBuilder.BuildNonQuery(this, NonQueryType.Insert);

                    int id = SQLExecuter.ExecuteNonQuery(sqlBuilder, NonQueryType.Insert);

                    if (PrimaryKey.Keys.Count == 1)
                    {
                        UpdateSinglePrimaryKey(id);
                    }
                    else
                    {
                        UpdateCombinedPrimaryKey();
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

        public virtual void Delete()
        {
            throw new NotImplementedException();
        }

        public RuntimeType ValueAs<RuntimeType>() where RuntimeType : ORMEntity
        {
            if (this is RuntimeType entity)
            {
                return entity;
            }

            throw new InvalidCastException($"Cannot convert object of type [{GetType().Name}] to type [{typeof(RuntimeType).Name}].");
        }

        public ORMEntity Inner() => default;

        public ORMEntity Left() => default;

        public ORMEntity Right() => default;

        public ORMEntity Full() => default;

        internal ORMEntity ShallowCopy()
        {
            return MemberwiseClone() as ORMEntity;
        }

        internal void UpdateSinglePrimaryKey(int id)
        {
            this[PrimaryKey.Keys[0].ColumnName] = id;
            PrimaryKey.Keys[0].Value = id;
        }

        internal void UpdateCombinedPrimaryKey()
        {
            throw new NotImplementedException();
        }
    }
}