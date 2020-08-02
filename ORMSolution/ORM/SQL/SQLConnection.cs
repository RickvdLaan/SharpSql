using Microsoft.Data.SqlClient;
using System;
using System.Threading;

namespace ORM
{
    internal class SQLExecuter
    {
        internal static AsyncLocal<SqlConnection> CurrentConnection { get; set; } = new AsyncLocal<SqlConnection>();

        internal static int ExecuteNonQuery(SQLBuilder sqlBuilder, NonQueryType nonQueryType)
        {
            if (!ORMUtilities.IsUnitTesting)
            {
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
                    command.Parameters.AddRange(sqlBuilder.SqlParameters);
                }

                return nonQueryType switch
                {
                    NonQueryType.Insert => (int)command.ExecuteScalar(),
                    NonQueryType.Update => command.ExecuteNonQuery(),
                    _ => throw new NotImplementedException(nonQueryType.ToString()),
                };
            }

            return 1;
        }

        internal static void ExecuteEntityQuery<EntityType>(EntityType entity, SQLBuilder sqlBuilder)
            where EntityType : ORMEntity
        {
            if (!ORMUtilities.IsUnitTesting)
            {
                using var connection = new SqlConnection(ORMUtilities.ConnectionString);
                CurrentConnection.Value = connection;

                using var command = new SqlCommand(sqlBuilder.GeneratedQuery, connection);
                command.Connection.Open();

                if (sqlBuilder.SqlParameters != null)
                {
                    command.Parameters.AddRange(sqlBuilder.SqlParameters);
                }

                using var reader = command.ExecuteReader();
                ORMUtilities.DataReader(entity, reader, sqlBuilder);
            }
        }

        internal static void ExecuteCollectionQuery<EntityType>(ORMCollection<EntityType> ormCollection, SQLBuilder sqlBuilder)
            where EntityType : ORMEntity
        {
            if (!ORMUtilities.IsUnitTesting)
            {
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
                    command.Parameters.AddRange(sqlBuilder.SqlParameters);
                }

                using var reader = command.ExecuteReader();
                ORMUtilities.DataReader<ORMCollection<EntityType>, EntityType>(ormCollection, reader, sqlBuilder);
            }
        }
    }
}
