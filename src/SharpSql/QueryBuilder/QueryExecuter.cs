using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;

namespace SharpSql
{
    internal class QueryExecuter
    {
        internal static AsyncLocal<SqlConnection> CurrentConnection { get; set; } = new AsyncLocal<SqlConnection>();

        internal static object ExecuteNonQuery(QueryBuilder queryBuilder, List<SqlParameter> sqlParameters)
        {
            if (UnitTestUtilities.IsUnitTesting)
            {
                if (queryBuilder.NonQueryType == NonQueryType.Delete)
                    return null;

                // The SQL server returns the unique id of the just inserted row, but during a unit test
                // nothing is actually inserted, thus returning 1.
                return 1;
            }

            using var connection = new SqlConnection(DatabaseUtilities.ConnectionString);
            CurrentConnection.Value = connection;

            using var command = new SqlCommand(queryBuilder.GeneratedQuery, connection);
            command.Connection.Open();

            if (DatabaseUtilities.Transaction.Value != null)
            {
                command.Transaction = DatabaseUtilities.Transaction.Value;
            }

            if (sqlParameters != null)
            {
                foreach (SqlParameter sqlParameter in sqlParameters)
                {
                    command.Parameters.Add(sqlParameter);
                }
            }

            var sqlReturnValue = queryBuilder.NonQueryType switch
            {
                NonQueryType.Insert => command.ExecuteScalar(),
                NonQueryType.Update => command.ExecuteNonQuery(),
                NonQueryType.Delete => command.ExecuteNonQuery(),
                _ => throw new NotImplementedException(queryBuilder.NonQueryType.ToString()),
            };

            if (sqlReturnValue != null 
             && sqlReturnValue != DBNull.Value)
            {
                // Auto increment
                return (int)sqlReturnValue;
            }

            // We return null because the PK doesn't use AutoIncrement.
            return null;
        }

        internal static object ExecuteNonQuery(QueryBuilder queryBuilder)
        {
            return ExecuteNonQuery(queryBuilder, queryBuilder.SqlParameters);
        }

        internal static void ExecuteEntityQuery<EntityType>(EntityType entity, QueryBuilder queryBuilder)
            where EntityType : SharpSqlEntity
        {
            if (UnitTestUtilities.IsUnitTesting)
            {
                UnitTestUtilities.ExecuteEntityQuery(entity, queryBuilder);
                return;
            }

            using var connection = new SqlConnection(DatabaseUtilities.ConnectionString);
            CurrentConnection.Value = connection;

            using var command = new SqlCommand(queryBuilder.GeneratedQuery, connection);
            command.Connection.Open();

            if (queryBuilder.SqlParameters != null)
            {
                foreach (SqlParameter sqlParameter in queryBuilder.SqlParameters)
                {
                    command.Parameters.Add(sqlParameter);
                }
            }

            using var reader = command.ExecuteReader();
            QueryMapper.DataReader(entity, reader, queryBuilder);
        }

        internal static void ExecuteCollectionQuery<EntityType>(SharpSqlCollection<EntityType> collection, QueryBuilder queryBuilder)
            where EntityType : SharpSqlEntity
        {
            if (UnitTestUtilities.IsUnitTesting)
            {
                UnitTestUtilities.ExecuteCollectionQuery(collection, queryBuilder);
                return;
            }

            using var connection = new SqlConnection(DatabaseUtilities.ConnectionString);
            CurrentConnection.Value = connection;

            using var command = new SqlCommand(queryBuilder.GeneratedQuery, connection);
            command.Connection.Open();

            if (DatabaseUtilities.Transaction.Value != null)
            {
                command.Transaction = DatabaseUtilities.Transaction.Value;
            }

            if (queryBuilder.SqlParameters != null)
            {
                foreach (SqlParameter sqlParameter in queryBuilder.SqlParameters)
                {
                    command.Parameters.Add(sqlParameter);
                }
            }

            using var reader = command.ExecuteReader();
            QueryMapper.DataReader<SharpSqlCollection<EntityType>, EntityType>(collection, reader, queryBuilder);
        }

        internal static DataTable GetDataTableScheme()
        {
            using var connection = new SqlConnection(DatabaseUtilities.ConnectionString);
            return connection.GetSchema();
        }
    }
}
