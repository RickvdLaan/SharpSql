using SharpSql.Attributes;
using SharpSql.UnitTests;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace SharpSql;

internal sealed class UnitTestUtilities
{
    internal static bool IsUnitTesting { get; set; }

    internal static void ChangeDataTableType(SharpSqlEntity entity, string columnName, ref object value)
    {
        ChangeDataTableType(entity.GetPropertyInfo(columnName), ref value);
    }

    internal static void ChangeDataTableType(PropertyInfo entityPropertyInfo, ref object value)
    {
        if (IsUnitTesting)
        {
            // Unit tests columns are all of type string, therefore they require to be converted to their respective type.
            if (Nullable.GetUnderlyingType(entityPropertyInfo.PropertyType) != null && value != DBNull.Value)
            {
                value = Convert.ChangeType(value, Nullable.GetUnderlyingType(entityPropertyInfo.PropertyType));
            }
            else if (!entityPropertyInfo.PropertyType.IsSubclassOf(typeof(SharpSqlEntity)) && value != DBNull.Value)
            {
                value = Convert.ChangeType(value, entityPropertyInfo.PropertyType);
            }
        }
    }

    internal static void PopulateChildEntity(ref string propertyName, ref object value, SharpSqlEntity childEntity)
    {
        propertyName = propertyName.Split('_').Last();

        var childPropertyType = childEntity.GetPropertyInfo(propertyName).PropertyType;

        // Unit tests columns are all of type string, therefore they require to be converted to their respective type.
        if (Nullable.GetUnderlyingType(childPropertyType) != null && value != DBNull.Value)
        {
            value = Convert.ChangeType(value, Nullable.GetUnderlyingType(childPropertyType));
        }
        else if (!childPropertyType.IsSubclassOf(typeof(SharpSqlEntity)) && value != DBNull.Value)
        {
            value = Convert.ChangeType(value, childPropertyType);
        }
    }

    internal static void ExecuteEntityQuery<EntityType>(SharpSqlEntity entity, QueryBuilder queryBuilder)
             where EntityType : SharpSqlEntity
    {
        if (!entity.PrimaryKey.IsCombinedPrimaryKey)
        {
            var primaryKey = entity.PrimaryKey.Keys[0];
            var tableName = SharpSqlUtilities.GetTableNameFromEntity(entity);
            var id = queryBuilder.SqlParameters.Where(x => x.SourceColumn == primaryKey.PropertyName).FirstOrDefault().Value;

            var reader = MemoryEntityDatabase.FetchEntityById(tableName, primaryKey, id);

            if (reader == null)
                throw new ArgumentException($"No record found for { primaryKey.PropertyName }: {id}.");

            reader = ApplyEntityJoinsToReader(entity, reader, queryBuilder);

            entity = ApplyEntityManyToManyToReader<EntityType>(entity, reader, queryBuilder);

            if (entity.ObjectState == ObjectState.New)
            {
                QueryMapper.DataReader(entity, reader, queryBuilder);
            }
        }
        else
        {
            var tableName = SharpSqlUtilities.GetTableNameFromEntity(entity);

            var ids = new List<object>(entity.PrimaryKey.Keys.Count);

            foreach (var key in entity.PrimaryKey.Keys)
            {
                ids.Add(queryBuilder.SqlParameters.Where(x => x.SourceColumn == key.PropertyName).FirstOrDefault().Value);
            }

            var reader = MemoryEntityDatabase.FetchEntityByCombinedId(tableName, entity.PrimaryKey, ids);

            if (reader == null)
                throw new ArgumentException($"No record found.");

            reader = ApplyEntityJoinsToReader(entity, reader, queryBuilder);

            QueryMapper.DataReader(entity, reader, queryBuilder);
        }
    }

    internal static void ExecuteCollectionQuery<EntityType>(SharpSqlCollection<EntityType> collection, QueryBuilder queryBuilder)
        where EntityType : SharpSqlEntity
    {
        // If an exception occurs, it just means you didn't specify the SharpSqlUnitTestAttribute to the Unit Test.
        var unitTestAttribute = new StackTrace().GetFrames().Select(x => x.GetMethod().GetCustomAttributes(typeof(SharpSqlUnitTestAttribute), false)).Where(x => x.Any()).First().First() as SharpSqlUnitTestAttribute;

        DataTable table = null;

        // IsManyToMany
        if (unitTestAttribute.MemoryTables[0].ColumnType == ColumnType.ManyToMany)
        {
            table = MemoryCollectionDatabase.Fetch(unitTestAttribute.MemoryTables[0].MemoryTableName);
        }
        else
        {
            table = MemoryCollectionDatabase.Fetch(unitTestAttribute.MemoryTables[0].MemoryTableName);
        }

        using var reader = table.CreateDataReader();

        QueryMapper.DataReader<SharpSqlCollection<EntityType>, EntityType>(collection as SharpSqlCollection<EntityType>, reader, queryBuilder);
    }

    internal static string GetMemoryTableName()
    {
        // If an exception occurs, it just means you didn't specify the SharpSqlUnitTestAttribute to the Unit Test.
        var unitTestAttribute = new StackTrace().GetFrames().Select(x => x.GetMethod().GetCustomAttributes(typeof(SharpSqlUnitTestAttribute), false)).Where(x => x.Any()).First().First() as SharpSqlUnitTestAttribute;

        return unitTestAttribute.MemoryTables[0].MemoryTableName;
    }

    private static SharpSqlEntity ApplyEntityManyToManyToReader<EntityType>(SharpSqlEntity entity, IDataReader reader, QueryBuilder queryBuilder)
        where EntityType : SharpSqlEntity
    {
        foreach (var join in queryBuilder.AllJoins)
        {
            if (join.IsManyToMany)
            {
                var properties = entity.GetType().GetProperties().Where(x => x.PropertyType == join.RightTableAttribute.CollectionTypeRight && x.CustomAttributes.Any(x => x.AttributeType == typeof(SharpSqlManyToManyAttribute)));

                var parentDataTable = new DataTable();
                parentDataTable.Load(reader);
                
                var m2mTableName = join.RightTableAttribute.EntityType.Name;
                var m2mEntity = Activator.CreateInstance(join.RightTableAttribute.EntityType, true);

                var collection = new SharpSqlCollection<EntityType>();

                ExecuteCollectionQuery(collection, queryBuilder);

                return collection.First();
            }
        }

        return entity;
    }

    private static IDataReader ApplyEntityJoinsToReader(SharpSqlEntity entity, IDataReader reader, QueryBuilder queryBuilder)
    {
        foreach (var join in queryBuilder.AllJoins)
        {

            if (!join.IsManyToMany)
            {
                foreach (var field in entity.TableScheme)
                {
                    if (join.LeftPropertyInfo.PropertyType == entity.GetPropertyInfo(field).PropertyType)
                    {
                        var parentDataTable = new DataTable();
                        parentDataTable.Load(reader);

                        var childTableName = SharpSqlUtilities.CollectionEntityRelations[join.LeftPropertyInfo.PropertyType].Name;
                        var childEntity = Activator.CreateInstance(join.LeftPropertyInfo.PropertyType);
                        var childId = parentDataTable.Rows[0][entity.TableScheme.IndexOf(field)];

                        var childReader = MemoryEntityDatabase.FetchEntityById(childTableName, entity.PrimaryKey.Keys[0], childId);

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
        }

        return reader;
    }
}