using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;

namespace SharpSql
{
    internal class SQLExecuter
    {
        internal static AsyncLocal<SqlConnection> CurrentConnection { get; set; } = new AsyncLocal<SqlConnection>();

        internal static object ExecuteNonQuery(SQLBuilder sqlBuilder, List<SqlParameter> sqlParameters)
        {
            if (UnitTestUtilities.IsUnitTesting)
            {
                if (sqlBuilder.NonQueryType == NonQueryType.Delete)
                    return null;

                // The SQL server returns the unique id of the just inserted row, but during a unit test
                // nothing is actually inserted, thus returning 1.
                return 1;
            }

            using var connection = new SqlConnection(DatabaseUtilities.ConnectionString);
            CurrentConnection.Value = connection;

            using var command = new SqlCommand(sqlBuilder.GeneratedQuery, connection);
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

            var sqlReturnValue = sqlBuilder.NonQueryType switch
            {
                NonQueryType.Insert => command.ExecuteScalar(),
                NonQueryType.Update => command.ExecuteNonQuery(),
                NonQueryType.Delete => command.ExecuteNonQuery(),
                _ => throw new NotImplementedException(sqlBuilder.NonQueryType.ToString()),
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

        internal static object ExecuteNonQuery(SQLBuilder sqlBuilder)
        {
            return ExecuteNonQuery(sqlBuilder, sqlBuilder.SqlParameters);
        }

        internal static void ExecuteEntityQuery<EntityType>(EntityType entity, SQLBuilder sqlBuilder)
            where EntityType : ORMEntity
        {
            if (UnitTestUtilities.IsUnitTesting)
            {
                UnitTestUtilities.ExecuteEntityQuery(entity, sqlBuilder);
                return;
            }

            using var connection = new SqlConnection(DatabaseUtilities.ConnectionString);
            CurrentConnection.Value = connection;

            using var command = new SqlCommand(sqlBuilder.GeneratedQuery, connection);
            command.Connection.Open();

            if (sqlBuilder.SqlParameters != null)
            {
                foreach (SqlParameter sqlParameter in sqlBuilder.SqlParameters)
                {
                    command.Parameters.Add(sqlParameter);
                }
            }

            using var reader = command.ExecuteReader();
            SQLHelper.DataReader(entity, reader, sqlBuilder);
        }

        internal static void ExecuteCollectionQuery<EntityType>(ORMCollection<EntityType> ormCollection, SQLBuilder sqlBuilder)
            where EntityType : ORMEntity
        {
            if (UnitTestUtilities.IsUnitTesting)
            {
                UnitTestUtilities.ExecuteCollectionQuery(ormCollection, sqlBuilder);
                return;
            }

            using var connection = new SqlConnection(DatabaseUtilities.ConnectionString);
            CurrentConnection.Value = connection;

            using var command = new SqlCommand(sqlBuilder.GeneratedQuery, connection);
            command.Connection.Open();

            if (DatabaseUtilities.Transaction.Value != null)
            {
                command.Transaction = DatabaseUtilities.Transaction.Value;
            }

            if (sqlBuilder.SqlParameters != null)
            {
                foreach (SqlParameter sqlParameter in sqlBuilder.SqlParameters)
                {
                    command.Parameters.Add(sqlParameter);
                }
            }

            using var reader = command.ExecuteReader();
            SQLHelper.DataReader<ORMCollection<EntityType>, EntityType>(ormCollection, reader, sqlBuilder);
        }

        internal static DataTable GetDataTableScheme()
        {
            using var connection = new SqlConnection(DatabaseUtilities.ConnectionString);
            return connection.GetSchema();
        }
    }
}
