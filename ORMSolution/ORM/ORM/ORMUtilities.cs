using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using ORM.Attributes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace ORM
{
    public sealed class ORMUtilities
    {
        internal static string ConnectionString { get; private set; }

        internal static bool IsUnitTesting { get; private set; }

        internal static ORMMemoryDatabase MemoryDatabase { get; set; }

        internal static Dictionary<Type, Type> CollectionEntityRelations { get; private set; }

        internal static Dictionary<(Type CollectionTypeLeft, Type CollectionTypeRight), ORMTableAttribute> ManyToManyRelations { get; private set; }

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
            ManyToManyRelations = new Dictionary<(Type CollectionTypeLeft, Type CollectionTypeRight), ORMTableAttribute>();
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

        public static CollectionType ExecuteDirectQuery<CollectionType, EntityType>(string query, bool disableChangeTracking = false, params object[] parameters)
            where CollectionType : ORMCollection<EntityType>, new()
            where EntityType : ORMEntity
        {
            var collection = ConvertTo<CollectionType, EntityType>(ExecuteDirectQuery(query, parameters), disableChangeTracking);

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

        private static DataTable ExecuteReader(SqlCommand command)
        {
            if (!IsUnitTesting)
            {
                using var reader = command.ExecuteReader();
                var dataTable = new DataTable();
                dataTable.Load(reader);

                return dataTable;
            }
            else
            {
                throw new NotImplementedException();

                //MemoryDatabase.Fetch(null, null);
            }
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
            using SqlConnection connection = new SqlConnection(ConnectionString);
            SQLExecuter.CurrentConnection.Value = connection;

            using var command = new SqlCommand(query, connection);
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
