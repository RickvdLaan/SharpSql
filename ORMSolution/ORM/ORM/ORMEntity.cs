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

        public bool IsNew { get; internal set; }

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

        internal string InternalPrimaryKeyName { get; set; }

        internal ORMEntity OriginalFetchedValue { get; set; } = null;

        internal List<ORMEntity> EntityRelations = new List<ORMEntity>();

        internal List<string> MutableTableScheme { get; set; }

        internal (string fieldName, bool isDirty)[] IsDirtyList { get; set; }

        private void UpdateIsDirtyList()
        {
            for (int i = 0; i < TableScheme.Count; i++)
            {
                if (TableScheme[i] == InternalPrimaryKeyName)
                {
                    continue;
                }

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

        protected ORMEntity(string primaryKeyName, bool disableChangeTracking = false)
        {
            InternalPrimaryKeyName = primaryKeyName;
            DisableChangeTracking = disableChangeTracking;
            IsNew = OriginalFetchedValue == null;

            if (!ORMUtilities.IsUnitTesting && !DisableChangeTracking)
            {
                IsDirtyList = new (string fieldName, bool isDirty)[TableScheme.Count - 1];
            }
        }

        public object this[string columnName]
        {
            get { return GetType().GetProperty(columnName, PublicIgnoreCaseFlags | NonPublicFlags).GetValue(this); }
            set { GetType().GetProperty(InternalPrimaryKeyName).SetValue(this, value); }
        }

        internal PropertyInfo GetPrimaryKeyPropertyInfo()
        {
            if (string.IsNullOrWhiteSpace(InternalPrimaryKeyName))
            {
                throw new ArgumentNullException($"PK-property \"{InternalPrimaryKeyName}\" can't be null or empty.");
            }

            var propertyInfo = GetType().GetProperty(InternalPrimaryKeyName);

            if (propertyInfo == null)
            {
                throw new ArgumentException($"No PK-property found for name: \"{InternalPrimaryKeyName}\" in {GetType().Name}.");
            }

            return propertyInfo;
        }

        protected void FetchEntityById<CollectionType, EntityType>(object id)
            where CollectionType : ORMCollection<EntityType>, new()
            where EntityType : ORMEntity
        {
            NewArrayExpression joinExpression = null;
            List<MethodCallExpression> joinExpressions = new List<MethodCallExpression>(TableScheme.Count);
            BinaryExpression whereExpression = null;

            foreach (var field in TableScheme)
            {
                var fieldPropertyInfo = GetType().GetProperty(field, PublicFlags);
                if (fieldPropertyInfo.PropertyType.IsSubclassOf(typeof(ORMEntity)))
                {
                    // Contains the join represented as a MemberExpression: {x.TableName}.
                    var joinMemberExpression = Expression.Property(Expression.Parameter(typeof(EntityType), $"x"), fieldPropertyInfo);

                    // Adds the Left() method to the current MemberExpression to tell the SQLBuilder
                    // what type of join is being used - resulting in: {x.TableName.Left()} of type MethodCallExpression.
                    // and adding the expression to the list of joins.
                    joinExpressions.Add(Expression.Call(joinMemberExpression, GetType().GetMethod(nameof(ORMEntity.Left))));
                }
            }

            // Combining the previously made join(s) into one NewArrayExpression.
            joinExpression = Expression.NewArrayInit(typeof(ORMEntity), joinExpressions);

            // Contains the id represented as a MemberExpression: {x.InternalPrimaryKeyName}.
            var memberExpression = Expression.Property(Expression.Parameter(typeof(EntityType), $"x"), GetPrimaryKeyPropertyInfo());

            // Contains the actual id represented as a ConstantExpression: {id_value}.
            var constantExpression = Expression.Constant(id, id.GetType());

            // Combines the expressions represtend as a Expression: {(x.InternalPrimaryKeyName == id_value)}
            whereExpression = Expression.Equal(memberExpression, constantExpression);

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

            IsNew = (int)this[InternalPrimaryKeyName] <= 0;

            if (!ORMUtilities.IsUnitTesting && IsNew)
                throw new Exception($"No [{GetType().Name}] found for [{InternalPrimaryKeyName}]: '{id}'.");

            ExecutedQuery = collection.ExecutedQuery;

            if (OriginalFetchedValue != null)
                OriginalFetchedValue.ExecutedQuery = ExecutedQuery;
        }

        internal void FetchEntityById<CollectionType, EntityType>(object id, List<string> tableScheme)
            where CollectionType : ORMCollection<EntityType>, new()
            where EntityType : ORMEntity
        {
            MutableTableScheme = tableScheme;

            this[InternalPrimaryKeyName] = id;

            FetchEntityById<CollectionType, EntityType>(id);
        }

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
                            
                            var entityRelationId = (int)relation[relation.InternalPrimaryKeyName];
                            var entityJoin       = this[relation.GetType().Name];

                            entityJoin.GetType().GetProperty(relation.InternalPrimaryKeyName).SetValue(entityJoin, entityRelationId);
                            entityJoin.GetType().GetProperty(nameof(ExecutedQuery)).SetValue(entityJoin, relation.ExecutedQuery);
                        }
                    }

                    if (IsNew && EntityRelations.Count == 0 || IsNew && EntityRelations.Any(r => r.IsNew))
                    {
                        sqlBuilder.BuildNonQuery(this, NonQueryType.Insert);

                        int id = connection.ExecuteNonQuery(sqlBuilder, NonQueryType.Insert);

                        this[InternalPrimaryKeyName] = id;
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