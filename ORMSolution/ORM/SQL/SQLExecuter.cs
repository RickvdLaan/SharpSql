using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;

namespace ORM
{
    internal class SQLExecuter
    {
        internal static AsyncLocal<SqlConnection> CurrentConnection { get; set; } = new AsyncLocal<SqlConnection>();

        internal static int ExecuteNonQuery(string generatedQuery, List<SqlParameter> sqlParameters, NonQueryType nonQueryType)
        {
            if (UnitTestUtilities.IsUnitTesting)
            {
                // The SQL server returns the unique id of the just inserted row, but during a unit test
                // nothing is actually inserted, thus returning 1.
                return 1;
            }

            using var connection = new SqlConnection(ORMUtilities.ConnectionString);
            CurrentConnection.Value = connection;

            using var command = new SqlCommand(generatedQuery, connection);
            command.Connection.Open();

            if (ORMUtilities.Transaction.Value != null)
            {
                command.Transaction = ORMUtilities.Transaction.Value;
            }

            if (sqlParameters != null)
            {
                foreach (SqlParameter sqlParameter in sqlParameters)
                {
                    command.Parameters.Add(sqlParameter);
                }
            }

            return nonQueryType switch
            {
                NonQueryType.Insert => (int)command.ExecuteScalar(),
                NonQueryType.Update => command.ExecuteNonQuery(),
                _ => throw new NotImplementedException(nonQueryType.ToString()),
            };
        }

        internal static int ExecuteNonQuery(SQLBuilder sqlBuilder, NonQueryType nonQueryType)
        {
            return ExecuteNonQuery(sqlBuilder.GeneratedQuery, sqlBuilder.SqlParameters, nonQueryType);
        }

        internal static void ExecuteEntityQuery<EntityType>(EntityType entity, SQLBuilder sqlBuilder)
            where EntityType : ORMEntity
        {
            if (UnitTestUtilities.IsUnitTesting)
            {
                UnitTestUtilities.ExecuteEntityQuery(entity, sqlBuilder);
                return;
            }

            using var connection = new SqlConnection(ORMUtilities.ConnectionString);
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

            using var connection = new SqlConnection(ORMUtilities.ConnectionString);
            CurrentConnection.Value = connection;

            using var command = new SqlCommand(sqlBuilder.GeneratedQuery, connection);
            command.Connection.Open();

            if (ORMUtilities.Transaction.Value != null)
            {
                command.Transaction = ORMUtilities.Transaction.Value;
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
            using var connection = new SqlConnection(ORMUtilities.ConnectionString);
            return connection.GetSchema();
        }
    }
}
