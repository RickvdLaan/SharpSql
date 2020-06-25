using Microsoft.Data.SqlClient;
using ORM.SQL;
using System;
using System.Data;
using System.Reflection;

namespace ORM
{
    public class ORMUtilities
    {
        public static CollectionType ExecuteDirectQuery<CollectionType, EntityType>(string query, params object[] parameters)
            where CollectionType : ORMCollection<EntityType>, new()
            where EntityType : ORMEntity
        {
            return ConvertTo<CollectionType, EntityType>(ExecuteDirectQuery(query, parameters));
        }

        public static DataTable ExecuteDirectQuery(string query, params object[] parameters)
        {
            using (var connection = new SQLConnection())
            {
                using (var command = new SqlCommand(query, connection.SqlConnection))
                {
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    using (var reader = command.ExecuteReader())
                    {
                        var dataTable = new DataTable();
                        dataTable.Load(reader);

                        return dataTable;
                    }
                }
            }
        }

        public static CollectionType ConvertTo<CollectionType, EntityType>(DataTable dataTable)
            where CollectionType : ORMCollection<EntityType>, new ()
            where EntityType : ORMEntity
        {
            var collection = new CollectionType();

            using (var reader = dataTable.CreateDataReader())
            {
                while (reader.Read())
                {
                    var entity = (ORMEntity)Activator.CreateInstance(typeof(EntityType));

                    for (int i = 0; i < reader.VisibleFieldCount; i++)
                    {
                        var entityPropertyInfo = typeof(EntityType).GetProperty(reader.GetName(i), BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                        if (null == entityPropertyInfo)
                        {
                            throw new NotImplementedException(string.Format("Column [{0}] has not been implemented in [{1}].", reader.GetName(i), typeof(EntityType).FullName));
                        }
                        else if (!entityPropertyInfo.CanWrite)
                        {
                            throw new ReadOnlyException(string.Format("Property [{0}] is read-only.", reader.GetName(i), typeof(EntityType).FullName));
                        }

                        entityPropertyInfo.SetValue(entity, reader.GetValue(i));
                    }

                    collection.Add(entity);
                }
            }

            return collection;
        }
    }
}
