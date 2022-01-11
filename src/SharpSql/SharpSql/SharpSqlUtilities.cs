using SharpSql.Attributes;
using SharpSql.UnitTests;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;

namespace SharpSql
{
    public sealed class SharpSqlUtilities
    {
        internal static bool AllowAnonymouseTypes { get; set; }

        internal static MemoryEntityDatabase MemoryEntityDatabase { get; set; }

        internal static MemoryCollectionDatabase MemoryCollectionDatabase { get; set; }

        internal static Dictionary<Type, Type> CollectionEntityRelations { get; private set; }

        internal static Dictionary<(Type CollectionTypeLeft, Type CollectionTypeRight), SharpSqlTableAttribute> ManyToManyRelations { get; private set; }

        internal static HashSet<(Type EntityType, string ColumnName)> UniqueConstraints { get; private set; }

        internal static Dictionary<Type, Dictionary<string, ColumnType>> CachedColumns { get; private set; }

        internal static Dictionary<Type, List<string>> CachedMutableColumns { get; private set; }

        internal static Dictionary<Type, byte> CachedManyToMany { get; private set; }

        public SharpSqlUtilities()
        {
            UnitTestUtilities.IsUnitTesting = new StackTrace().GetFrames().Any(x => x.GetMethod().ReflectedType.GetCustomAttributes(typeof(SharpSqlUnitTestAttribute), false).Any());
            CollectionEntityRelations = new Dictionary<Type, Type>();
            ManyToManyRelations = new Dictionary<(Type CollectionTypeLeft, Type CollectionTypeRight), SharpSqlTableAttribute>();
            UniqueConstraints = new HashSet<(Type EntityType, string ColumnName)>();
            CachedColumns = new Dictionary<Type, Dictionary<string, ColumnType>>();
            CachedMutableColumns = new Dictionary<Type, List<string>>();
            CachedManyToMany = new Dictionary<Type, byte>();
        }

        public static CollectionType ConvertTo<CollectionType, EntityType>(DataTable dataTable, bool disableChangeTracking)
            where CollectionType : SharpSqlCollection<EntityType>, new()
            where EntityType : SharpSqlEntity
        {
            var collection = new CollectionType()
            {
                DisableChangeTracking = disableChangeTracking
            };

            using (var reader = dataTable.CreateDataReader())
            {
                QueryMapper.DataReader<CollectionType, EntityType>(collection, reader, null);
            }

            return collection;
        }

        public static string GetTableNameFromEntity(SharpSqlEntity entity)
        {
            return CollectionEntityRelations[entity.GetType()].Name;
        }
    }
}
