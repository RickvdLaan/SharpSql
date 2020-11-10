using Microsoft.Data.SqlClient;
using ORM.Attributes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace ORM
{
    internal class SQLExecuter
    {
        internal static AsyncLocal<SqlConnection> CurrentConnection { get; set; } = new AsyncLocal<SqlConnection>();

        internal static int ExecuteNonQuery(string generatedQuery, List<SqlParameter> sqlParameters, NonQueryType nonQueryType)
        {
            if (!ORMUtilities.IsUnitTesting)
            {
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
            else
            {
                // The SQL server returns the unique id of the just inserted row, but during a unit test
                // nothing is actually inserted, thus returning 1.
                return 1;
            }
        }

        internal static int ExecuteNonQuery(SQLBuilder sqlBuilder, NonQueryType nonQueryType)
        {
            return ExecuteNonQuery(sqlBuilder.GeneratedQuery, sqlBuilder.SqlParameters, nonQueryType);
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
                    foreach (SqlParameter sqlParameter in sqlBuilder.SqlParameters)
                    {
                        command.Parameters.Add(sqlParameter);
                    }
                }

                using var reader = command.ExecuteReader();
                SQLHelper.DataReader(entity, reader, sqlBuilder);
            }
            else
            {
                if (entity.PrimaryKey.Keys.Count == 1)
                {
                    var tableName = ORMUtilities.CollectionEntityRelations[entity.GetType()].Name;
                    var id = sqlBuilder.SqlParameters.Where(x => x.SourceColumn == entity.PrimaryKey.Keys[0].ColumnName).FirstOrDefault().Value;

                    var reader = ORMUtilities.MemoryEntityDatabase.FetchEntityById(tableName, entity.PrimaryKey, id);

                    if (reader == null)
                        throw new ArgumentException($"No record found for {entity.PrimaryKey.Keys[0].ColumnName}: {id}.");

                    SQLHelper.DataReader(entity, reader, sqlBuilder);
                }
                else
                {
                    // Combined primary key.
                    throw new NotImplementedException();
                }
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
                    foreach (SqlParameter sqlParameter in sqlBuilder.SqlParameters)
                    {
                        command.Parameters.Add(sqlParameter);
                    }
                }

                using var reader = command.ExecuteReader();
                SQLHelper.DataReader<ORMCollection<EntityType>, EntityType>(ormCollection, reader, sqlBuilder);
            }
            else
            {
                var unitTestAttribute = new StackTrace().GetFrames().Select(x => x.GetMethod().GetCustomAttributes(typeof(ORMUnitTestAttribute), false)).Where(x => x.Any()).First().First() as ORMUnitTestAttribute;
                var table = ORMUtilities.MemoryCollectionDatabase.Fetch(unitTestAttribute.MemoryTableName);

                using var reader = table.CreateDataReader();

                SQLHelper.DataReader<ORMCollection<EntityType>, EntityType>(ormCollection, reader, sqlBuilder);
            }
        }
    }
}
