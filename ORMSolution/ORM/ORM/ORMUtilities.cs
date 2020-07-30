using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using ORM.Attributes;
using ORM.Exceptions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;

namespace ORM
{
    public sealed class ORMUtilities
    {
        internal static string ConnectionString { get; private set; }

        internal static bool IsUnitTesting { get; private set; }

        internal static Dictionary<Type, Type> CollectionEntityRelations { get; private set; }

        internal static Dictionary<Type, (Type CollectionTypeLeft, Type CollectionTypeRight)> ManyToManyRelations { get; private set; }

        internal static Dictionary<Type, List<string>> CachedColumns { get; private set; }

        internal static AsyncLocal<SqlTransaction> Transaction { get; private set; } = new AsyncLocal<SqlTransaction>();

        public ORMUtilities(IConfiguration configuration = null) 
            : this()
        {
            if (configuration != null)
            {
                ConnectionString = configuration.GetConnectionString("DefaultConnection");
            }
        }

        public ORMUtilities()
        {
            IsUnitTesting = new StackTrace().GetFrames().Any(x => x.GetMethod().ReflectedType.GetCustomAttributes(typeof(ORMUnitTestAttribute), false).Any());
            CollectionEntityRelations = new Dictionary<Type, Type>();
            ManyToManyRelations = new Dictionary<Type, (Type CollectionTypeLeft, Type CollectionTypeRight)>();
            CachedColumns = new Dictionary<Type, List<string>>();
        }

        public static bool IsInTransaction()
        {
            return Transaction.Value != null;
        }

        public static void TransactionBegin()
        {
            Transaction.Value = SQLExecuter.CurrentConnection.Value.BeginTransaction();
        }

        public static void TransactionCommit(bool rollbackTransactionOnFailure = false)
        {
            if (IsInTransaction())
            {
                try
                {
                    Transaction.Value.Commit();
                }
                catch
                {
                    if (rollbackTransactionOnFailure)
                    {
                        Transaction.Value.Rollback();
                    }

                    throw;
                }
                finally
                {
                    DisposeTransaction();
                }
            }
        }

        public static void TransactionRollback()
        {
            if (IsInTransaction())
            {
                Transaction.Value.Rollback();

                DisposeTransaction();
            }
        }

        private static void DisposeTransaction()
        {
            if (IsInTransaction())
            {
                Transaction.Value.Dispose();
                Transaction.Value = null;
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

        public static int ExecuteDirectNonQuery(string query, params object[] parameters)
        {
            return ExecuteQuery(ExecuteWriter, query, parameters);
        }

        public static DataTable ExecuteDirectQuery(string query, params object[] parameters)
        {
            return ExecuteQuery(ExecuteReader, query, parameters);
        }

        private static DataTable ExecuteReader(SqlCommand command)
        {
            if (!IsUnitTesting)
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

        private static int ExecuteWriter(SqlCommand command)
        {
            if (Transaction.Value != null)
            {
                command.Transaction = Transaction.Value;
            }

            return command.ExecuteNonQuery();
        }

        private static T ExecuteQuery<T>(Func<SqlCommand, T> method, string query, params object[] parameters)
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                SQLExecuter.CurrentConnection.Value = connection;

                using (var command = new SqlCommand(query, connection))
                {
                    if (!IsUnitTesting)
                    {
                        command.Connection.Open();
                    }

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

                    if (Transaction.Value != null)
                    {
                        command.Transaction = Transaction.Value;
                    }

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        command.Parameters.Add(new SqlParameter(regexMatches[i], parameters[i]));
                    }

                    return method.Invoke(command);
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
            where CollectionType : ORMCollection<EntityType>
            where EntityType : ORMEntity
        {
            while (reader.Read())
            {
                var entity = (EntityType)Activator.CreateInstance(typeof(EntityType));
                entity.DisableChangeTracking = collection.DisableChangeTracking;

                PopulateEntity(entity, reader, sqlBuilder);

                collection.Add(entity);
            }
        }

        internal static void DataReader<EntityType>(EntityType entity, DbDataReader reader, SQLBuilder sqlBuilder)
            where EntityType : ORMEntity
        {
            while (reader.Read())
            {
                PopulateEntity(entity, reader, sqlBuilder);
            }
        }

        internal static void PopulateEntity<EntityType>(EntityType entity, DbDataReader reader, SQLBuilder sqlBuilder)
            where EntityType : ORMEntity
        {
            if (sqlBuilder?.TableNameResolvePaths.Count > 0)
            {
                BuildMultiLayeredEntity(entity, reader, sqlBuilder);
            }
            else
            {
                for (int i = 0; i < reader.VisibleFieldCount; i++)
                {
                    SetEntityProperty(entity, reader, i);
                }
            }

            // Once the entity is populated we can check whether or not this is a new or existing data row.
            entity.IsNew = entity.PrimaryKey.Keys.Any(x => (int)entity[x.ColumnName] <= 0);

            if (!entity.DisableChangeTracking)
            {
                entity.GetType()
                      .GetProperty(nameof(ORMEntity.OriginalFetchedValue), entity.NonPublicFlags)
                      .SetValue(entity, entity.ShallowCopy());
            }
        }

        private static void BuildMultiLayeredEntity<EntityType>(EntityType entity, DbDataReader reader, SQLBuilder sqlBuilder)
            where EntityType : ORMEntity
        {
            var tableIndex = 0;

            foreach (var (name, _) in sqlBuilder.TableOrder)
            {
                var tableColumnCount = sqlBuilder.TableNameColumnCount[name];

                if (!sqlBuilder.TableNameColumnCount.ContainsKey(name)
                  || tableColumnCount == 0)
                {
                    continue;
                }
                for (int i = 0; i < tableColumnCount; i++)
                {
                    if (sqlBuilder.TableNameResolvePaths.ContainsKey(name))
                    {
                        SetEntityProperty(GetObjectAtPath(entity, sqlBuilder.TableNameResolvePaths[name]), reader, i, tableIndex);
                    }
                    else
                    {
                        SetEntityProperty(entity, reader, i, tableIndex);
                    }
                }

                tableIndex += tableColumnCount;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetEntityProperty(ORMEntity entity, DbDataReader reader, int iteration, int tableIndex = 0)
        {
            var propertyName = reader.GetName(iteration + tableIndex);
            var entityPropertyInfo = entity.GetType().GetProperty(propertyName, entity.PublicIgnoreCaseFlags)
                                  ?? entity.GetType().GetProperties().FirstOrDefault(x => (x.GetCustomAttributes(typeof(ORMColumnAttribute), true).FirstOrDefault() as ORMColumnAttribute)?.ColumnName == propertyName);

            if (null == entityPropertyInfo)
            {
                if (propertyName == entity.GetType().Name)
                {
                    throw new ORMIllegalColumnNameException($"The column [{propertyName}] has not been implemented in entity [{entity.GetType().Name}], but can't have the same name as its enclosing type.");
                }

                throw new NotImplementedException($"The column [{propertyName}] has not been implemented in entity [{entity.GetType().Name}].");
            }
            else if (!entityPropertyInfo.CanWrite)
            {
                throw new ReadOnlyException($"Property [{propertyName}] is read-only in [{entity.GetType().Name}].");
            }

            object value;

            switch (entityPropertyInfo.PropertyType)
            {
                case Type type when type == typeof(DateTime?):
                    value = reader.GetValue(iteration);
                    break;
                case Type type when type == typeof(DateTime):
                    if (reader.GetValue(iteration) == DBNull.Value)
                    {
                        throw new ORMPropertyNotNullableException($"Property {propertyName} is not nullable, but the database column equivelant is.");
                    }

                    value = reader.GetValue(iteration);
                    break;
                case Type type when type.IsSubclassOf(typeof(ORMEntity)):
                    var subEntity = Activator.CreateInstance(type.UnderlyingSystemType);

                    var fetchEntityByPrimaryKey = subEntity.GetType().BaseType
                        .GetMethod(nameof(ORMEntity.FetchEntityByPrimaryKey),
                        BindingFlags.Instance | BindingFlags.NonPublic,
                        Type.DefaultBinder,
                        new Type[] { typeof(object) },
                        null);

                    value = fetchEntityByPrimaryKey.Invoke(subEntity, new object[] { reader.GetValue(iteration) });

                    entity.EntityRelations.Add(value as ORMEntity);
                    break;
                default:
                    value = reader.GetValue(iteration);
                    break;
            }

            if (entityPropertyInfo.PropertyType.IsGenericType && entityPropertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if (reader.GetValue(iteration) == DBNull.Value)
                {
                    entityPropertyInfo.SetValue(entity, null);
                }
            }
            else
            {
                entityPropertyInfo.SetValue(entity, value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ORMEntity GetObjectAtPath(ORMEntity entity, string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                foreach (var step in path.Split('.'))
                {
                    var property = entity.GetType().GetProperty(step, entity.PublicIgnoreCaseFlags);

                    var value = property.GetValue(entity);
                    if (value == null)
                    {
                        value = Activator.CreateInstance(property.PropertyType);
                        property.SetValue(entity, value);
                    }

                    entity = (ORMEntity)value;
                }
            }
            return entity;
        }
    }
}
