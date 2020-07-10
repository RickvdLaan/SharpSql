using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using ORM.Attributes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace ORM
{
    public sealed class ORMUtilities
    {
        internal static string ConnectionString { get; private set; }

        internal static Dictionary<Type, Type> CollectionEntityRelations { get; private set; }

        internal static Dictionary<Type, (Type CollectionTypeLeft, Type CollectionTypeRight)> ManyToManyRelations { get; private set; }

        public ORMUtilities(IConfiguration configuration) : this()
        {
            ConnectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public ORMUtilities()
        {
            CollectionEntityRelations = new Dictionary<Type, Type>();
            ManyToManyRelations = new Dictionary<Type, (Type CollectionTypeLeft, Type CollectionTypeRight)>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    var attributes = type.GetCustomAttributes(typeof(ORMTableAttribute), true);
                    if (attributes.Length > 0)
                    {
                        var tableAttribute = (attributes.First() as ORMTableAttribute);

                        if (tableAttribute.CollectionTypeLeft == null
                         && tableAttribute.CollectionTypeRight == null)
                        {
                            CollectionEntityRelations.Add(tableAttribute.CollectionType, tableAttribute.EntityType);
                            CollectionEntityRelations.Add(tableAttribute.EntityType, tableAttribute.CollectionType);
                        }
                        else
                        {
                            ManyToManyRelations.Add(tableAttribute.CollectionType, (tableAttribute.CollectionTypeLeft, tableAttribute.CollectionTypeRight));
                        }
                    }
                }
            }
        }

        public static CollectionType ExecuteDirectQuery<CollectionType, EntityType>(string query, params object[] parameters)
            where CollectionType : ORMCollection<EntityType>, new()
            where EntityType : ORMEntity
        {
            var collection = ConvertTo<CollectionType, EntityType>(ExecuteDirectQuery(query, parameters));

            collection.ExecutedQuery = query;

            return collection;
        }

        public static DataTable ExecuteDirectQuery(string query, params object[] parameters)
        {
            using (var connection = new SQLConnection())
            {
                using (var command = new SqlCommand(query, connection.SqlConnection))
                {
                    var regexMatches = Regex.Matches(query, @"\@[^ |\))]\w+")
                        .OfType<Match>()
                        .Select(m => m.Groups[0].Value)
                        .Distinct()
                        .ToList();

                    if (parameters.FirstOrDefault() == null && regexMatches.Count > 0 
                     || parameters.Length != regexMatches.Count)
                    {
                        throw new ArgumentException(string.Format("{0} unique parameter{1} found, but {2} parameter{3} provided.",
                            regexMatches.Count,
                            regexMatches.Count > 1 || regexMatches.Count == 0 ? "s were" : " was",
                            parameters.Length,
                            parameters.Length > 1 || parameters.Length == 0 ? "s were" : " was"));
                    }

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        command.Parameters.Add(new SqlParameter(regexMatches[i], parameters[i]));
                    }

                    if (!IsUnitTesting())
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            var dataTable = new DataTable();
                            dataTable.Load(reader);

                            return dataTable;
                        }
                    }

                    return new DataTable();
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
                DataReader<CollectionType, EntityType>(collection, reader);
            }

            return collection;
        }

        internal static void DataReader<CollectionType, EntityType>(CollectionType collection, DbDataReader reader)
            where CollectionType : ORMCollection<EntityType>, new()
            where EntityType : ORMEntity
        {
            while (reader.Read())
            {
                var entity = (ORMEntity)Activator.CreateInstance(typeof(EntityType));

                EntityReader(entity, reader);

                collection.Add(entity);
            }
        }

        internal static void DataReader<EntityType>(EntityType entity, DbDataReader reader)
            where EntityType : ORMEntity
        {
            while (reader.Read())
            {
                EntityReader(entity, reader);
            }
        }

        internal static void EntityReader<EntityType>(EntityType entity, DbDataReader reader)
        {
            for (int i = 0; i < reader.VisibleFieldCount; i++)
            {
                var entityPropertyInfo = entity.GetType().GetProperty(reader.GetName(i), BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                if (null == entityPropertyInfo)
                {
                    throw new NotImplementedException($"Column [{reader.GetName(i)}] has not been implemented in [{entity.GetType().Name}].");
                }
                else if (!entityPropertyInfo.CanWrite)
                {
                    throw new ReadOnlyException($"Property [{reader.GetName(i)}] is read-only in [{entity.GetType().Name}].");
                }

                entityPropertyInfo.SetValue(entity, reader.GetValue(i));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsUnitTesting() =>
            new StackTrace().GetFrames().Any(x => x.GetMethod().ReflectedType.GetCustomAttributes(typeof(ORMUnitTestAttribute), false).Any());
    }
}
