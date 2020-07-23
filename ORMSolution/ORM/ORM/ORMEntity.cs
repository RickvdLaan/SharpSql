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

                if (!ORMUtilities.IsUnitTesting)
                    UpdateIsDirtyList();

                return IsDirtyList.Any(x => x.isDirty == true);
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

        public ORMCombinedPrimaryKey PrimaryKey { get; internal set; }

        internal ORMEntity OriginalFetchedValue { get; set; } = null;

        internal List<ORMEntity> EntityRelations = new List<ORMEntity>();

        internal List<string> MutableTableScheme { get; set; }

        internal (string fieldName, bool isDirty)[] IsDirtyList { get; set; }

        private void UpdateIsDirtyList()
        {
            for (int i = 0; i < TableScheme.Count; i++)
            {
                if (PrimaryKey.Keys.Any(x => x.ColumnName == TableScheme[i]))
                    continue;

                var thisValue = this[TableScheme[i]];
                var originalValue = OriginalFetchedValue[TableScheme[i]];

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

        protected ORMEntity(bool disableChangeTracking = false)
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

            PrimaryKey = new ORMCombinedPrimaryKey(attributes.Count);

            for (int i = 0; i < attributes.Count; i++)
            {
                PrimaryKey.Add(attributes[i].Name, null);
            }

            DisableChangeTracking = disableChangeTracking;
            IsNew = OriginalFetchedValue == null;

            if (!ORMUtilities.IsUnitTesting && !DisableChangeTracking)
            {
                IsDirtyList = new (string fieldName, bool isDirty)[TableScheme.Count - PrimaryKey.Keys.Count];
            }
        }

        public object this[string columnName]
        {
            get { return GetType().GetProperty(columnName, PublicIgnoreCaseFlags | NonPublicFlags).GetValue(this); }
            set { GetType().GetProperty(PrimaryKey.Keys.FirstOrDefault(x => x.ColumnName == columnName).ColumnName).SetValue(this, value); }
        }

        internal PropertyInfo[] GetPrimaryKeyPropertyInfo()
        {
            PropertyInfo[] propertyInfo = new PropertyInfo[PrimaryKey.Keys.Count];

            for (int i = 0; i < PrimaryKey.Keys.Count; i++)
            {
                propertyInfo[i] = GetType().GetProperty(PrimaryKey.Keys[i].ColumnName);

                if (propertyInfo[i] == null)
                {
                    throw new ArgumentException($"No PK-property found for name: \"{PrimaryKey.Keys[i]}\" in {GetType().Name}.");
                }
            }

            return propertyInfo;
        }

        protected void FetchEntityById<CollectionType, EntityType>(object id)
            where CollectionType : ORMCollection<EntityType>, new()
            where EntityType : ORMEntity
        {
            PrimaryKey.Keys[0].Value = id;

            FetchEntityByCombinedPrimaryKey<CollectionType, EntityType>(PrimaryKey);
        }

        protected void FetchEntityByCombinedPrimaryKey<CollectionType, EntityType>(params object[] primaryKey)
        where CollectionType : ORMCollection<EntityType>, new()
        where EntityType : ORMEntity
        {
            for (int i = 0; i < primaryKey.Length; i++)
            {
                PrimaryKey.Keys[i].Value = primaryKey[i];
            }

            FetchEntityByCombinedPrimaryKey<CollectionType, EntityType>(PrimaryKey);
        }

        private void FetchEntityByCombinedPrimaryKey<CollectionType, EntityType>(ORMCombinedPrimaryKey combinedPrimaryKey)
            where CollectionType : ORMCollection<EntityType>, new()
            where EntityType : ORMEntity
        {
            NewArrayExpression joinExpression = null;
            List<MethodCallExpression> joinExpressions = new List<MethodCallExpression>(TableScheme.Count);
            BinaryExpression whereExpression = null;

            foreach (var field in TableScheme)
            {
                var fieldPropertyInfo = GetType().GetProperty(field, PublicFlags);

                if (fieldPropertyInfo != null && fieldPropertyInfo.PropertyType.IsSubclassOf(typeof(ORMEntity)))
                {
                    // Contains the join represented as a MemberExpression: {x.TableName}.
                    var joinMemberExpression = Expression.Property(Expression.Parameter(typeof(EntityType), $"x"), fieldPropertyInfo);

                    // Adds the Left() method to the current MemberExpression to tell the SQLBuilder
                    // what type of join is being used - resulting in: {x.TableName.Left()} of type MethodCallExpression.
                    // and adding the expression to the list of joins.
                    joinExpressions.Add(Expression.Call(joinMemberExpression, GetType().GetMethod(nameof(ORMEntity.Left))));

                    continue;
                }
            }

            // Combining the previously made join(s) into one NewArrayExpression.
            joinExpression = Expression.NewArrayInit(typeof(ORMEntity), joinExpressions);

            for (int i = 0; i < PrimaryKey.Keys.Count; i++)
            {
                // Contains the id represented as a MemberExpression: {x.InternalPrimaryKeyName}.
                var memberExpression = Expression.Property(Expression.Parameter(typeof(EntityType), $"x"), GetPrimaryKeyPropertyInfo()[i]);

                // Contains the actual id represented as a ConstantExpression: {id_value}.
                var constantExpression = Expression.Constant(combinedPrimaryKey.Keys[i].Value, combinedPrimaryKey.Keys[i].Value.GetType());

                // Combines the expressions represtend as a Expression: {(x.InternalPrimaryKeyName == id_value)}
                if (whereExpression == null)
                    whereExpression = Expression.Equal(memberExpression, constantExpression);
                else
                    whereExpression = Expression.AndAlso(whereExpression, Expression.Equal(memberExpression, constantExpression));
            }

            var collection = new CollectionType();
            collection.InternalJoin(joinExpression);
            collection.InternalWhere(whereExpression);
            collection.Fetch(this, 1);

            foreach (MethodCallExpression expression in joinExpression.Expressions)
            {
                var fieldPropertyInfo = GetType().GetProperty((expression.Object as MemberExpression).Member.Name, PublicFlags);
                if (fieldPropertyInfo.GetValue(this) is ORMEntity entityColumnJoin && fieldPropertyInfo.PropertyType.IsSubclassOf(typeof(ORMEntity)))
                {
                    EntityRelations.Add(entityColumnJoin);
                }
            }

            IsNew = PrimaryKey.Keys.Any(x => (int)this[x.ColumnName] <= 0);

            if (!ORMUtilities.IsUnitTesting && IsNew)
                throw new Exception($"No [{GetType().Name}] found for {string.Join(", ", PrimaryKey.Keys.Select(x => x.ToString()).ToArray())}.");

            ExecutedQuery = collection.ExecutedQuery;

            if (OriginalFetchedValue != null)
                OriginalFetchedValue.ExecutedQuery = ExecutedQuery;
        }

        #region NUnit

        internal void FetchEntityById<CollectionType, EntityType>(object id, List<string> tableScheme)
            where CollectionType : ORMCollection<EntityType>, new()
            where EntityType : ORMEntity
        {
            MutableTableScheme = tableScheme;

            this[PrimaryKey.Keys[0].ColumnName] = id;

            FetchEntityById<CollectionType, EntityType>(id);
        }

        internal void FetchEntityByCombinedPrimaryKey<CollectionType, EntityType>(ORMCombinedPrimaryKey combinedPrimaryKey, List<string> tableScheme)
            where CollectionType : ORMCollection<EntityType>, new()
            where EntityType : ORMEntity
        {
            MutableTableScheme = tableScheme;

            for (int i = 0; i < combinedPrimaryKey.Keys.Count; i++)
            {
                this[combinedPrimaryKey.Keys[i].ColumnName] = combinedPrimaryKey.Keys[i].Value;
            }

            FetchEntityByCombinedPrimaryKey<CollectionType, EntityType>(combinedPrimaryKey);
        }

        #endregion

        public virtual void Save()
        {
            if (IsDirty)
            {
                using (var connection = new SQLConnection())
                {
                    var sqlBuilder = new SQLBuilder();

                    foreach (var relation in EntityRelations)
                    {
                        if (relation.IsDirty)
                        {
                            relation.Save();

                            connection.OpenConnection();

                            for (int i = 0; i < relation.PrimaryKey.Keys.Count; i++)
                            {
                                var entityRelationId = (int)relation[relation.PrimaryKey.Keys[i].ColumnName];
                                var entityJoin = this[relation.GetType().Name];

                                entityJoin.GetType().GetProperty(relation.PrimaryKey.Keys[i].ColumnName).SetValue(entityJoin, entityRelationId);
                                entityJoin.GetType().GetProperty(nameof(ExecutedQuery)).SetValue(entityJoin, relation.ExecutedQuery);
                            }
                        }
                    }

                    if (IsNew && EntityRelations.Count == 0 || IsNew && EntityRelations.Any(r => r.IsNew))
                    {
                        sqlBuilder.BuildNonQuery(this, NonQueryType.Insert);

                        int id = connection.ExecuteNonQuery(sqlBuilder, NonQueryType.Insert);

                        // @Todo: @bug: needs to be fixed for combined primary keys.
                        this[PrimaryKey.Keys[0].ColumnName] = id;
                    }
                    else
                    {
                        sqlBuilder.BuildNonQuery(this, NonQueryType.Update);
                        connection.ExecuteNonQuery(sqlBuilder, NonQueryType.Update);
                    }

                    ExecutedQuery = sqlBuilder.GeneratedQuery;
                }
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