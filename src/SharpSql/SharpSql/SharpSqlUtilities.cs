using SharpSql.UnitTests;
using System.Data;
using System.Diagnostics;
using System.Linq;

namespace SharpSql;

public sealed class SharpSqlUtilities
{
    internal static MemoryEntityDatabase MemoryEntityDatabase { get; set; }

    internal static MemoryCollectionDatabase MemoryCollectionDatabase { get; set; }

    public SharpSqlUtilities()
    {
        UnitTestUtilities.IsUnitTesting = new StackTrace().GetFrames().Any(x => x.GetMethod().ReflectedType.GetCustomAttributes(typeof(SharpSqlUnitTestAttribute), false).Any());
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
}