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

                if (!ORMUtilities.IsUnitTesting())
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

                if (thisValue != null
                && !thisValue.Equals(OriginalFetchedValue.GetType().GetProperty(TableScheme[i], PublicFlags)
                                                         .GetValue(OriginalFetchedValue)))
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

            if (!ORMUtilities.IsUnitTesting() && !DisableChangeTracking)
            {
                IsDirtyList = new (string fieldName, bool isDirty)[TableScheme.Count - 1];
            }
        }

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
            var propertyInfo = GetPrimaryKeyPropertyInfo();

            var left = Expression.Property(Expression.Parameter(typeof(EntityType), $"x"), propertyInfo);
            var right = Expression.Constant(id, id.GetType());

            var collection = new CollectionType();
            collection.InternalWhere(Expression.Equal(left, right));
            collection.Fetch(this, 1);

            if (!ORMUtilities.IsUnitTesting() && IsNew)
                throw new Exception($"No [{GetType().Name}] found for [{InternalPrimaryKeyName}]: '{id}'.");

            ExecutedQuery = collection.ExecutedQuery;
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

        internal ORMEntity ShallowCopy()
        {
            return MemberwiseClone() as ORMEntity;
        }
    }
}