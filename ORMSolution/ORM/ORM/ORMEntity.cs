using ORM.Interfaces;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ORM
{
    public class ORMEntity : ORMObject, IORMEntity
    {
        internal string[] InternalFields { get; set; }

        internal ORMEntity OriginalFetchedValue { get; set; } = null;

        internal BindingFlags PublicFlags => BindingFlags.Instance | BindingFlags.Public;

        internal BindingFlags PublicIgnoreCaseFlags => PublicFlags | BindingFlags.IgnoreCase;

        internal BindingFlags NonPublicFlags => BindingFlags.Instance | BindingFlags.NonPublic;

        public string ExecutedQuery { get; internal set; } = "An unknown query has been executed.";

        public bool IsDirty
        {
            get
            {
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

        internal (string fieldName, bool isDirty)[] IsDirtyList { get; set; }

        private string InternalPrimaryKeyName { get; set; }

        private void UpdateIsDirtyList()
        {
            for (int i = 0; i < InternalFields.Length; i++)
            {
                if (InternalFields[i] == InternalPrimaryKeyName)
                {
                    continue;
                }

                var thisValue = GetType().GetProperty(InternalFields[i], PublicFlags)
                                         .GetValue(this);

                if (thisValue != null
                && !thisValue.Equals(OriginalFetchedValue.GetType().GetProperty(InternalFields[i], PublicFlags)
                                                         .GetValue(OriginalFetchedValue)))
                {
                    IsDirtyList[i - 1] = (InternalFields[i], true);
                }
                else
                {
                    IsDirtyList[i - 1] = (InternalFields[i], false);
                }
            }
        }

        protected ORMEntity(string primaryKeyName)
        {
            InternalPrimaryKeyName = primaryKeyName;
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