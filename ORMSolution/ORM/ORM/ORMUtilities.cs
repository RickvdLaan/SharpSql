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

        internal static Dictionary<Type, List<string>> CachedColumns { get; private set; }

        public ORMUtilities(IConfiguration configuration = null) : this()
        {
            if (configuration != null)
            {
                ConnectionString = configuration.GetConnectionString("DefaultConnection");
            }
        }

        public ORMUtilities()
        {
            CollectionEntityRelations = new Dictionary<Type, Type>();
            ManyToManyRelations = new Dictionary<Type, (Type CollectionTypeLeft, Type CollectionTypeRight)>();
            CachedColumns = new Dictionary<Type, List<string>>();
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
                DataReader<CollectionType, EntityType>(collection, reader, null);
            }

            return collection;
        }

        internal static void DataReader<CollectionType, EntityType>(CollectionType collection, DbDataReader reader, SQLBuilder sqlBuilder)
            where CollectionType : ORMCollection<EntityType>, new()
            where EntityType : ORMEntity
        {
            while (reader.Read())
            {
                var entity = (ORMEntity)Activator.CreateInstance(typeof(EntityType));

                EntityReader(entity, reader, sqlBuilder);

                collection.Add(entity);
            }
        }

        internal static void DataReader<EntityType>(EntityType entity, DbDataReader reader, SQLBuilder sqlBuilder)
            where EntityType : ORMEntity
        {
            while (reader.Read())
            {
                EntityReader(entity, reader, sqlBuilder);
            }
        }

        internal static void EntityReader<EntityType>(EntityType entity, DbDataReader reader, SQLBuilder sqlBuilder)
            where EntityType : ORMEntity
        {
            var tableScheme = CachedColumns[entity.GetType()];
            entity.InternalFields = new string[tableScheme.Count];
            entity.InternalFields = tableScheme.ToArray();
            entity.IsDirtyList = new (string fieldName, bool isDirty)[entity.InternalFields.Length - 1];

            if (sqlBuilder?.TableNameResolvePaths.Count > 0)
            {
                BuildMultiLayeredEntity(entity, reader, sqlBuilder);
                entity.GetType()
                  .GetProperty(nameof(ORMEntity.OriginalFetchedValue), entity.NonPublicFlags)
                  .SetValue(entity, entity.ShallowCopy());
                return;
            }

            for (int i = 0; i < reader.VisibleFieldCount; i++)
            {
                
                    var propertyName = reader.GetName(i);
                    var entityPropertyInfo = entity.GetType().GetProperty(propertyName, entity.PublicIgnoreCaseFlags);

                    if (null == entityPropertyInfo)
                    {
                        throw new NotImplementedException($"Column [{propertyName}] has not been implemented in [{entity.GetType().Name}].");
                    }
                    else if (!entityPropertyInfo.CanWrite)
                    {
                        throw new ReadOnlyException($"Property [{propertyName}] is read-only in [{entity.GetType().Name}].");
                    }
                    else if (entityPropertyInfo.PropertyType.IsSubclassOf(typeof(ORMEntity)))
                    {
                        continue;
                    }

                    entityPropertyInfo.SetValue(entity, reader.GetValue(i));
                
            }

            entity.GetType()
                  .GetProperty(nameof(ORMEntity.OriginalFetchedValue), entity.NonPublicFlags)
                  .SetValue(entity, entity.ShallowCopy());
        }

        private static void BuildMultiLayeredEntity<EntityType>(EntityType entity, DbDataReader reader, SQLBuilder sqlBuilder)
            where EntityType : ORMEntity
        {
            var tableIndex = 0;
            foreach (var table in sqlBuilder.TableOrder)
            {
                if (!sqlBuilder.TableNameColumnCount.ContainsKey(table.name))
                {
                    continue;
                }

                var tableColumnCount = sqlBuilder.TableNameColumnCount[table.name];
                if(tableColumnCount == 0)
                {
                    continue;
                }

                object obj;
                if (sqlBuilder.TableNameResolvePaths.ContainsKey(table.name))
                {
                    obj = GetObjectAtPath(entity, sqlBuilder.TableNameResolvePaths[table.name]);
                }
                else
                {
                    obj = entity;
                }

                for (int i = 0; i < tableColumnCount; i++)
                {
                    var propertyName = reader.GetName(tableIndex + i);
                    var entityPropertyInfo = obj.GetType().GetProperty(propertyName, entity.PublicIgnoreCaseFlags);

                    if (null == entityPropertyInfo)
                    {
                        throw new NotImplementedException($"Column [{propertyName}] has not been implemented in [{entity.GetType().Name}].");
                    }
                    else if (!entityPropertyInfo.CanWrite)
                    {
                        throw new ReadOnlyException($"Property [{propertyName}] is read-only in [{entity.GetType().Name}].");
                    }
                    else if (entityPropertyInfo.PropertyType.IsSubclassOf(typeof(ORMEntity)))
                    {
                        continue;
                    }

                    entityPropertyInfo.SetValue(obj, reader.GetValue(i));
                }

                tableIndex += tableColumnCount;
            } 


        }

        private static object GetObjectAtPath(object obj, string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                foreach (var step in path.Split('.'))
                {
                    var property = obj.GetType().GetProperty(step, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

                    var value = property.GetValue(obj);
                    if (value == null)
                    {
                        value = Activator.CreateInstance(property.PropertyType);
                        property.SetValue(obj, value);
                    }

                    obj = value;
                }
            }
            return obj;
        }

        internal static T[] ConcatArrays<T>(params T[][] arrays)
        {
            var position = 0;
            var outputArray = new T[arrays.Sum(a => a.Length)];
            foreach (var curr in arrays)
            {
                Array.Copy(curr, 0, outputArray, position, curr.Length);
                position += curr.Length;
            }
            return outputArray;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsUnitTesting() =>
            new StackTrace().GetFrames().Any(x => x.GetMethod().ReflectedType.GetCustomAttributes(typeof(ORMUnitTestAttribute), false).Any());
    }
}
