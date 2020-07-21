using ORM.Interfaces;
using System;
using System.Collections.Generic;
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

        public bool IsNew => OriginalFetchedValue == null;

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

        private List<string> _tableScheme = null;
        public List<string> TableScheme
        {
            get
            {
                if (_tableScheme == null)
                    _tableScheme = ORMUtilities.CachedColumns[GetType()];

                return _tableScheme;
            }
            set { _tableScheme = value; }
        }

        public bool DisableChangeTracking { get; internal set; }

        internal ORMEntity OriginalFetchedValue { get; set; } = null;

        internal string InternalPrimaryKeyName { get; set; }

        internal (string fieldName, bool isDirty)[] IsDirtyList { get; set; }

        private void UpdateIsDirtyList()
        {
            for (int i = 0; i < TableScheme.Count; i++)
            {
                if (TableScheme[i] == InternalPrimaryKeyName)
                {
                    continue;
                }

                var thisValue = GetType().GetProperty(TableScheme[i], PublicFlags)
                                         .GetValue(this);

                var originalValue = OriginalFetchedValue.GetType().GetProperty(TableScheme[i], PublicFlags)
                    .GetValue(OriginalFetchedValue);


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

            if (!ORMUtilities.IsUnitTesting && !DisableChangeTracking)
            {
                IsDirtyList = new (string fieldName, bool isDirty)[TableScheme.Count - 1];
            }
        }

        public object this[string columnName] { get { throw new NotImplementedException(); } }

        internal PropertyInfo GetPrimaryKeyPropertyInfo()
        {
            if (string.IsNullOrWhiteSpace(InternalPrimaryKeyName))
            {
                throw new ArgumentNullException($"PK-property \"{InternalPrimaryKeyName}\" can't be null or empty.");
            }

            var propertyInfo = GetType().GetProperties().Where(x => x.Name == InternalPrimaryKeyName).FirstOrDefault();

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
            Expression joinExpression = null;
            BinaryExpression whereExpression = null;

            foreach (var field in TableScheme)
            {
                var fieldPropertyInfo = GetType().GetProperty(field, PublicFlags);
                if (fieldPropertyInfo.PropertyType.IsSubclassOf(typeof(ORMEntity)))
                {
                    // Contains the join represented in a MemberExpression: {x.TableName}.
                    var joinMemberExpression = Expression.Property(Expression.Parameter(typeof(EntityType), $"x"), fieldPropertyInfo);

                    // Adds the Left() method to the current MemberExpression to tell the SQLBuilder
                    // what type of join it is used - resulting in: {x.TableName.Left()} of type MethodCallExpression.
                    var joinMethodCallExpression = Expression.Call(joinMemberExpression, GetType().GetMethod(nameof(ORMEntity.Left)));

                    if (joinExpression == null)
                    {
                        joinExpression = joinMethodCallExpression;
                    }
                    else
                    {
                        // Combining the previously made join with the next join into a NewArrayExpression.
                        joinExpression = Expression.NewArrayInit(typeof(ORMEntity), new List<Expression>() { joinExpression, joinMethodCallExpression });
                    }
                }
            }
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

            if (!ORMUtilities.IsUnitTesting && IsNew)
                throw new Exception($"No [{GetType().Name}] found for [{InternalPrimaryKeyName}]: '{id}'.");

            ExecutedQuery = collection.ExecutedQuery;
        }

        internal void FetchEntityById<CollectionType, EntityType>(object id, List<string> tableScheme)
            where CollectionType : ORMCollection<EntityType>, new()
            where EntityType : ORMEntity
        {
            TableScheme = tableScheme;

            FetchEntityById<CollectionType, EntityType>(id);
        }

        public virtual void Save()
        {
            if (IsDirty)
            {
                using (var connection = new SQLConnection())
                {
                    var sqlBuilder = new SQLBuilder();

                    if (IsNew)
                        sqlBuilder.BuildNonQuery(this, NonQueryType.Insert);
                    else
                        sqlBuilder.BuildNonQuery(this, NonQueryType.Update);

                    connection.ExecuteNonQuery(sqlBuilder);

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