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
                    var id = sqlBuilder.SqlParameters.Where(x => x.SourceColumn == entity.PrimaryKey.Keys[0].PropertyName).FirstOrDefault().Value;

                    var reader = ORMUtilities.MemoryEntityDatabase.FetchEntityById(tableName, entity.PrimaryKey.Keys[0], id);

                    if (reader == null)
                        throw new ArgumentException($"No record found for {entity.PrimaryKey.Keys[0].PropertyName}: {id}.");

                    reader = ApplyJoinsToReader(entity, reader, sqlBuilder);

                    SQLHelper.DataReader(entity, reader, sqlBuilder);
                }
                else
                {
                    var tableName = ORMUtilities.CollectionEntityRelations[entity.GetType()].Name;

                    var ids = new List<object>(entity.PrimaryKey.Keys.Count);

                    foreach (var key in entity.PrimaryKey.Keys)
                    {
                        ids.Add(sqlBuilder.SqlParameters.Where(x => x.SourceColumn == key.PropertyName).FirstOrDefault().Value);
                    }

                    var reader = ORMUtilities.MemoryEntityDatabase.FetchEntityByCombinedId(tableName, entity.PrimaryKey, ids);

                    if (reader == null)
                        throw new ArgumentException($"No record found.");

                    reader = ApplyJoinsToReader(entity, reader, sqlBuilder);

                    SQLHelper.DataReader(entity, reader, sqlBuilder);
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

        private static IDataReader ApplyJoinsToReader(ORMEntity entity, IDataReader reader, SQLBuilder sqlBuilder)
        {
            foreach (var join in sqlBuilder.Joins)
            {
                foreach (var field in entity.TableScheme)
                {
                    if (join.LeftPropertyInfo.PropertyType == entity.GetType().GetProperty(field).PropertyType)
                    {
                        var parentDataTable = new DataTable();
                        parentDataTable.Load(reader);

                        var childTableName = ORMUtilities.CollectionEntityRelations[join.LeftPropertyInfo.PropertyType].Name;
                        var childEntity = Activator.CreateInstance(join.LeftPropertyInfo.PropertyType);
                        var childId = parentDataTable.Rows[0][entity.TableScheme.IndexOf(field)];

                        var childReader = ORMUtilities.MemoryEntityDatabase.FetchEntityById(childTableName, entity.PrimaryKey.Keys[0], childId);

                        var childDataTable = new DataTable();
                        childDataTable.Load(childReader);

                        foreach (DataColumn column in childDataTable.Columns)
                        {
                            if (parentDataTable.Columns.Contains(column.ColumnName))
                            {
                                childDataTable.Columns[column.ColumnName].ColumnName = $"{ childDataTable.TableName }_{ column.ColumnName }";
                            }
                        }

                        parentDataTable.Merge(childDataTable);

                        var left = parentDataTable.Rows[0];
                        var right = parentDataTable.Rows[1];

                        foreach (DataColumn column in left.Table.Columns)
                        {
                            if (left[column] == DBNull.Value)
                                left[column] = right[column];
                        }

                        parentDataTable.Rows.Remove(right);

                        reader = parentDataTable.CreateDataReader();
                        break;
                    }
                }
            }

            return reader;
        }
    }
}
