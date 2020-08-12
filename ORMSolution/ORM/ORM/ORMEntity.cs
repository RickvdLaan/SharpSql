using ORM.Attributes;
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
    public class ORMEntity : ORMObject, IORMEntity
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
            for (int i = 0; i < TableScheme.Count; i++)
            {
                if (PrimaryKey.Keys.Any(x => x.ColumnName == TableScheme[i]))
                    continue;

                var thisValue = this[TableScheme[i]];
                var originalValue = OriginalFetchedValue?[TableScheme[i]];

                if ((thisValue != null && !thisValue.Equals(originalValue))
                 || (thisValue == null && originalValue != null))
                {
                    IsDirtyList[i - 1] = (TableScheme[i], true);
                }
                else
                {
                    IsDirtyList[i - 1] = (TableScheme[i], false);
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

            for (int i = 0; i < attributes.Count; i++)
            {
                PrimaryKey.Add(attributes[i].Name, null);
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
            set { GetType().GetProperty(PrimaryKey.Keys.FirstOrDefault(x => x.ColumnName == columnName).ColumnName).SetValue(this, value); }
        }

        internal PropertyInfo[] GetPrimaryKeyPropertyInfo()
        {
            PropertyInfo[] propertyInfo = new PropertyInfo[PrimaryKey.Count];

            for (int i = 0; i < PrimaryKey.Count; i++)
            {
                propertyInfo[i] = GetType().GetProperty(PrimaryKey.Keys[i].ColumnName);

                if (propertyInfo[i] == null)
                {
                    throw new ArgumentException($"No PK-property found for name: \"{PrimaryKey.Keys[i]}\" in {GetType().Name}.");
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

        private ORMEntity FetchDynamicEntity(ORMPrimaryKey combinedPrimaryKey)
        {
            BinaryExpression whereExpression = null;

            for (int i = 0; i < PrimaryKey.Count; i++)
            {
                // Contains the id represented as a MemberExpression: {x.InternalPrimaryKeyName}.
                var memberExpression = Expression.Property(Expression.Parameter(GetType(), $"x"), GetPrimaryKeyPropertyInfo()[i]);

                // Contains the actual id represented as a ConstantExpression: {id_value}.
                var constantExpression = Expression.Constant(combinedPrimaryKey.Keys[i].Value, combinedPrimaryKey.Keys[i].Value.GetType());

                // Combines the expressions represtend as a Expression: {(x.InternalPrimaryKeyName == id_value)}
                if (whereExpression == null)
                    whereExpression = Expression.Equal(memberExpression, constantExpression);
                else
                    whereExpression = Expression.AndAlso(whereExpression, Expression.Equal(memberExpression, constantExpression));
            }

            dynamic collection = Activator.CreateInstance(ORMUtilities.CollectionEntityRelations[GetType()]);
            collection.InternalWhere(whereExpression);
            collection.Fetch(this, 1);

            if (!ORMUtilities.IsUnitTesting && IsNew)
                throw new Exception($"No [{GetType().Name}] found for {string.Join(", ", PrimaryKey.Keys.Select(x => x.ToString()).ToArray())}.");

            ExecutedQuery = collection.ExecutedQuery;

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
                    if ((this[relation.GetType().Name] as ORMEntity).IsDirty)
                    {
                        (this[relation.GetType().Name] as ORMEntity).Save();

                        for (int i = 0; i < relation.PrimaryKey.Count; i++)
                        {
                            var entityRelationId = (int)(this[relation.GetType().Name] as ORMEntity)[relation.PrimaryKey.Keys[i].ColumnName];
                            var entityJoin = this[relation.GetType().Name];

                            //entityJoin.GetType().GetProperty(relation.PrimaryKey.Keys[i].ColumnName).SetValue(entityJoin, entityRelationId);
                            //entityJoin.GetType().GetProperty(nameof(ExecutedQuery)).SetValue(entityJoin, (this[relation.GetType().Name] as ORMEntity).ExecutedQuery);
                        }
                    }
                }

                if (IsNew && EntityRelations.Count == 0 || IsNew && EntityRelations.Any(r => r.IsNew))
                {
                    sqlBuilder.BuildNonQuery(this, NonQueryType.Insert);

                    int id = SQLExecuter.ExecuteNonQuery(sqlBuilder, NonQueryType.Insert);

                    // @Todo: @bug: needs to be fixed for combined primary keys.
                    this[PrimaryKey.Keys[0].ColumnName] = id;
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

        public ORMEntity Inner() => default;

        public ORMEntity Left() => default;

        public ORMEntity Right() => default;

        public ORMEntity Full() => default;

        internal ORMEntity ShallowCopy()
        {
            return MemberwiseClone() as ORMEntity;
        }
    }
}