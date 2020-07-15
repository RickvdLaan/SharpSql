using ORM.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ORM
{
    public class ORMEntity : ORMObject, IORMEntity
    {
        public string ExecutedQuery { get; internal set; } = "An unknown query has been executed.";


        public bool IsDirty
        {
            get
            {
                if (DisableChangeTracking)
                    return true;

                // IsDirty can't be unit tested because it requires a database connection to
                // determine its underlying fields. Using the IsDirty property in a ORMUnitTest will
                // return a NullReferenceException.
                // 
                // We could use ORMUtilities.IsUnitTesting() to provide a clearer exception message
                // but that doesn't change the underlying problem, therefore the costs of using
                // ORMUtilities.IsUnitTesting() don't outweigh the benefits.
                UpdateIsDirtyList();

                return IsDirtyList.Any(x => x.isDirty == true);
            }
        }

        public bool DisableChangeTracking { get; internal set; }

        internal ORMEntity OriginalFetchedValue { get; set; } = null;

        internal List<string> TableScheme => ORMUtilities.CachedColumns[GetType()];

        private string InternalPrimaryKeyName { get; set; }

        private (string fieldName, bool isDirty)[] IsDirtyList { get; set; }

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

            ExecutedQuery = collection.ExecutedQuery;
        }

        public virtual void Save()
        {
            if (IsDirty)
            {
                throw new NotImplementedException();
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