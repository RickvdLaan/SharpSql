using SharpSql.Attributes;
using SharpSql.UnitTests;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace SharpSql
{
    internal sealed class UnitTestUtilities
    {
        internal static bool IsUnitTesting { get; set; }

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

        internal static void ExecuteEntityQuery<EntityType>(EntityType entity, QueryBuilder queryBuilder)
                 where EntityType : SharpSqlEntity
        {
            if (!entity.PrimaryKey.IsCombinedPrimaryKey)
            {
                var primaryKey = entity.PrimaryKey.Keys[0];
                var tableName = SharpSqlUtilities.GetTableNameFromEntity(entity);
                var id = queryBuilder.SqlParameters.Where(x => x.SourceColumn == primaryKey.PropertyName).FirstOrDefault().Value;

                var reader = SharpSqlUtilities.MemoryEntityDatabase.FetchEntityById(tableName, primaryKey, id);

                if (reader == null)
                    throw new ArgumentException($"No record found for { primaryKey.PropertyName }: {id}.");

                reader = ApplyEntityJoinsToReader(entity, reader, queryBuilder);

                QueryMapper.DataReader(entity, reader, queryBuilder);
            }
            else
            {
                var tableName = SharpSqlUtilities.GetTableNameFromEntity(entity);

                var ids = new List<object>(entity.PrimaryKey.Keys.Count);

                foreach (var key in entity.PrimaryKey.Keys)
                {
                    ids.Add(queryBuilder.SqlParameters.Where(x => x.SourceColumn == key.PropertyName).FirstOrDefault().Value);
                }

                var reader = SharpSqlUtilities.MemoryEntityDatabase.FetchEntityByCombinedId(tableName, entity.PrimaryKey, ids);

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
            if (unitTestAttribute.MemoryTables.Count > 1)
            {
                var tableName = unitTestAttribute.MemoryTables.Where(x => x.EntityType == collection.TableAttribute.EntityType).First().MemoryTableName;

                table = SharpSqlUtilities.MemoryCollectionDatabase.Fetch(tableName);
            }
            else
            {
                table = SharpSqlUtilities.MemoryCollectionDatabase.Fetch(unitTestAttribute.MemoryTables[0].MemoryTableName);
            }

            using var reader = table.CreateDataReader();

            QueryMapper.DataReader<SharpSqlCollection<EntityType>, EntityType>(collection, reader, queryBuilder);
        }

        internal static string GetMemoryTableName()
        {
            // If an exception occurs, it just means you didn't specify the SharpSqlUnitTestAttribute to the Unit Test.
            var unitTestAttribute = new StackTrace().GetFrames().Select(x => x.GetMethod().GetCustomAttributes(typeof(SharpSqlUnitTestAttribute), false)).Where(x => x.Any()).First().First() as SharpSqlUnitTestAttribute;

            return unitTestAttribute.MemoryTables[0].MemoryTableName;
        }

        private static IDataReader ApplyEntityJoinsToReader(SharpSqlEntity entity, IDataReader reader, QueryBuilder queryBuilder)
        {
            foreach (var join in queryBuilder.Joins)
            {
                if (join.IsManyToMany)
                {
                    if (join.LeftPropertyInfo.ReflectedType == entity.GetType())
                    {
                        foreach (var property in entity.GetType().GetProperties())
                        {
                            if (property.PropertyType == join.RightTableAttribute.CollectionTypeRight
                             && property.CustomAttributes.Any(x => x.AttributeType == typeof(SharpSqlManyToMany)))
                            {
                                var parentDataTable = new DataTable();
                                parentDataTable.Load(reader);

                                var childTableName = join.RightTableAttribute.EntityType.Name;
                                var childEntity = Activator.CreateInstance(join.RightTableAttribute.EntityType, true) as SharpSqlEntity;
                                var childCollection = Activator.CreateInstance(join.RightTableAttribute.CollectionType) as IEnumerable<SharpSqlEntity>;

                                foreach (var childProperty in childEntity.GetType().GetProperties(childEntity.PublicFlags))
                                {
                                    var executedQuery = (string)childCollection.GetType().GetProperty(nameof(SharpSqlCollection<SharpSqlEntity>.ExecutedQuery)).GetValue(childCollection);

                                    if (!string.IsNullOrEmpty(executedQuery))
                                        break;

                                    if (childProperty.GetCustomAttributes(typeof(SharpSqlForeignKeyAttribute), false).FirstOrDefault() is SharpSqlForeignKeyAttribute fkAttribute && fkAttribute.Relation == entity.GetType())
                                    {
                                        BinaryExpression whereExpression = null;

                                        for (int i = 0; i < childEntity.PrimaryKey.Count; i++)
                                        {
                                            if (!childEntity.IsForeignKeyOfType(childEntity.PrimaryKey.Keys[i].PropertyName, entity.GetType()))
                                                continue;

                                            // Contains the id represented as a MemberExpression: {x.InternalPropertyName}.
                                            var memberExpression = Expression.Property(Expression.Parameter(childEntity.GetType(), "x"), childEntity.GetPrimaryKeyPropertyInfo()[i]);

                                            // Contains the actual id represented as a ConstantExpression: {id_value}.
                                            var constantExpression = Expression.Constant(entity.PrimaryKey.Keys[i].Value, entity.PrimaryKey.Keys[i].Value.GetType());

                                            // Combines the expressions represtend as a Expression: {(x.InternalPropertyName == id_value)}
                                            if (whereExpression == null)
                                                whereExpression = Expression.Equal(memberExpression, constantExpression);
                                        }

                                        // Sets the InternalWhere with the WhereExpression.
                                        childCollection.GetType().GetMethod(nameof(SharpSqlCollection<SharpSqlEntity>.InternalWhere), entity.NonPublicFlags, null, new Type[] { typeof(BinaryExpression) }, null).Invoke(childCollection, new object[] { whereExpression });

                                        // Fetches the data.
                                        childCollection.GetType().GetMethod(nameof(SharpSqlCollection<SharpSqlEntity>.Fetch), childEntity.NonPublicFlags, null, new Type[] { typeof(SharpSqlEntity), typeof(long), typeof(Expression) }, null).Invoke(childCollection, new object[] { null, -1, null });
                                    }
                                }

                                var childCollectionRight = Activator.CreateInstance(join.RightTableAttribute.CollectionTypeRight) as IEnumerable<SharpSqlEntity>;
                                var childEntityRight = Activator.CreateInstance(SharpSqlUtilities.CollectionEntityRelations[childCollectionRight.GetType()]) as SharpSqlEntity;

                                BinaryExpression whereExpressionRight = null;

                                foreach (var childEntityLeft in childCollection)
                                {
                                    for (int i = 0; i < childEntityLeft.PrimaryKey.Count; i++)
                                    {
                                        if (!childEntity.IsForeignKeyOfType(childEntityLeft.PrimaryKey.Keys[i].PropertyName, childEntityRight.GetType()))
                                            continue;

                                        // Contains the id represented as a MemberExpression: {x.InternalPropertyName}.
                                        var memberExpressionRight = Expression.Property(Expression.Parameter(childEntityRight.GetType(), "x"), childEntityRight.GetPrimaryKeyPropertyInfo()[0]);

                                        // Contains the actual id represented as a ConstantExpression: {id_value}.
                                        var constantExpressionRight = Expression.Constant(childEntityLeft.PrimaryKey.Keys[i].Value, childEntityLeft.PrimaryKey.Keys[i].Value.GetType());

                                        // Combines the expressions represtend as a Expression: {(x.InternalPropertyName == id_value)}
                                        if (whereExpressionRight == null)
                                            whereExpressionRight = Expression.Equal(memberExpressionRight, constantExpressionRight);
                                        else
                                            whereExpressionRight = Expression.Or(whereExpressionRight, Expression.Equal(memberExpressionRight, constantExpressionRight));
                                    }
                                }

                                // We no longer need the old collection;
                                childCollection = Activator.CreateInstance(join.RightTableAttribute.CollectionTypeRight) as IEnumerable<SharpSqlEntity>;

                                // Sets the InternalWhere with the WhereExpression.
                                childCollection.GetType().GetMethod(nameof(SharpSqlCollection<SharpSqlEntity>.InternalWhere), entity.NonPublicFlags, null, new Type[] { typeof(BinaryExpression) }, null).Invoke(childCollection, new object[] { whereExpressionRight });

                                // Fetches the data.
                                childCollection.GetType().GetMethod(nameof(SharpSqlCollection<SharpSqlEntity>.Fetch), entity.NonPublicFlags, null, new Type[] { typeof(SharpSqlEntity), typeof(long), typeof(Expression) }, null).Invoke(childCollection, new object[] { null, -1, null });

                                // Sets the ManyToMany collection.
                                property.SetValue(entity, childCollection);

                                reader = parentDataTable.CreateDataReader();
                                break;
                            }
                        }
                    }
                }
                else
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

                            var childReader = SharpSqlUtilities.MemoryEntityDatabase.FetchEntityById(childTableName, entity.PrimaryKey.Keys[0], childId);

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
}
