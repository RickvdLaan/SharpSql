using ORM.Attributes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;

namespace ORM
{
    public sealed class ORMUtilities
    {
        internal static MemoryEntityDatabase MemoryEntityDatabase { get; set; }

        internal static MemoryCollectionDatabase MemoryCollectionDatabase { get; set; }

        internal static Dictionary<Type, Type> CollectionEntityRelations { get; private set; }

        internal static Dictionary<(Type CollectionTypeLeft, Type CollectionTypeRight), ORMTableAttribute> ManyToManyRelations { get; private set; }

        internal static HashSet<(Type EntityType, string ColumnName)> UniqueConstraints { get; private set; }

        internal static Dictionary<Type, List<string>> CachedColumns { get; private set; }

        internal static Dictionary<Type, List<string>> CachedMutableColumns { get; private set; }

        public ORMUtilities()
        {
            UnitTestUtilities.IsUnitTesting = new StackTrace().GetFrames().Any(x => x.GetMethod().ReflectedType.GetCustomAttributes(typeof(ORMUnitTestAttribute), false).Any());
            CollectionEntityRelations = new Dictionary<Type, Type>();
            ManyToManyRelations = new Dictionary<(Type CollectionTypeLeft, Type CollectionTypeRight), ORMTableAttribute>();
            UniqueConstraints = new HashSet<(Type EntityType, string ColumnName)>();
            CachedColumns = new Dictionary<Type, List<string>>();
            CachedMutableColumns = new Dictionary<Type, List<string>>();
        }

        public static CollectionType ConvertTo<CollectionType, EntityType>(DataTable dataTable, bool disableChangeTracking)
            where CollectionType : ORMCollection<EntityType>, new()
            where EntityType : ORMEntity
        {
            var collection = new CollectionType()
            {
                DisableChangeTracking = disableChangeTracking
            };

            using (var reader = dataTable.CreateDataReader())
            {
                SQLHelper.DataReader<CollectionType, EntityType>(collection, reader, null);
            }

            return collection;
        }

        public static string GetTableNameFromEntity(ORMEntity entity)
        {
            return CollectionEntityRelations[entity.GetType()].Name;
        }
    }
}
