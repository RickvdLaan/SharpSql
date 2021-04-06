using ORM.Attributes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace ORM
{
    internal sealed class UnitTestUtilities
    {
        internal static bool IsUnitTesting { get; set; }

        internal static void PopulateChildEntity(ref string propertyName, ref object value, ORMEntity childEntity)
        {
            propertyName = propertyName.Split('_').Last();

            var childPropertyType = childEntity.GetPropertyInfo(propertyName).PropertyType;

            // Unit tests columns are all of type string, therefore they require to be converted to their respective type.
            if (Nullable.GetUnderlyingType(childPropertyType) != null && value != DBNull.Value)
            {
                value = Convert.ChangeType(value, Nullable.GetUnderlyingType(childPropertyType));
            }
            else if (!childPropertyType.IsSubclassOf(typeof(ORMEntity)) && value != DBNull.Value)
            {
                value = Convert.ChangeType(value, childPropertyType);
            }
        }

        internal static void ExecuteEntityQuery<EntityType>(EntityType entity, SQLBuilder sqlBuilder)
                 where EntityType : ORMEntity
        {
            if (!entity.PrimaryKey.IsCombinedPrimaryKey)
            {
                var primaryKey = entity.PrimaryKey.Keys[0];
                var tableName = ORMUtilities.GetTableNameFromEntity(entity);
                var id = sqlBuilder.SqlParameters.Where(x => x.SourceColumn == primaryKey.PropertyName).FirstOrDefault().Value;

                var reader = ORMUtilities.MemoryEntityDatabase.FetchEntityById(tableName, primaryKey, id);

                if (reader == null)
                    throw new ArgumentException($"No record found for { primaryKey.PropertyName }: {id}.");

                reader = ApplyEntityJoinsToReader(entity, reader, sqlBuilder);

                SQLHelper.DataReader(entity, reader, sqlBuilder);
            }
            else
            {
                var tableName = ORMUtilities.GetTableNameFromEntity(entity);

                var ids = new List<object>(entity.PrimaryKey.Keys.Count);

                foreach (var key in entity.PrimaryKey.Keys)
                {
                    ids.Add(sqlBuilder.SqlParameters.Where(x => x.SourceColumn == key.PropertyName).FirstOrDefault().Value);
                }

                var reader = ORMUtilities.MemoryEntityDatabase.FetchEntityByCombinedId(tableName, entity.PrimaryKey, ids);

                if (reader == null)
                    throw new ArgumentException($"No record found.");

                reader = ApplyEntityJoinsToReader(entity, reader, sqlBuilder);

                SQLHelper.DataReader(entity, reader, sqlBuilder);
            }
        }

        internal static void ExecuteCollectionQuery<EntityType>(ORMCollection<EntityType> ormCollection, SQLBuilder sqlBuilder)
            where EntityType : ORMEntity
        {
            // If an exception occurs, it just means you didn't specify the ORMUnitTestAttribute to the Unit Test.
            var unitTestAttribute = new StackTrace().GetFrames().Select(x => x.GetMethod().GetCustomAttributes(typeof(ORMUnitTestAttribute), false)).Where(x => x.Any()).First().First() as ORMUnitTestAttribute;

            DataTable table = null;

            // IsManyToMany
            if (unitTestAttribute.MemoryTables.Count > 1)
            {
                var tableName = unitTestAttribute.MemoryTables.Where(x => x.EntityType == ormCollection.TableAttribute.EntityType).First().MemoryTableName;

                table = ORMUtilities.MemoryCollectionDatabase.Fetch(tableName);
            }
            else
            {
                table = ORMUtilities.MemoryCollectionDatabase.Fetch(unitTestAttribute.MemoryTables[0].MemoryTableName);
            }

            using var reader = table.CreateDataReader();

            SQLHelper.DataReader<ORMCollection<EntityType>, EntityType>(ormCollection, reader, sqlBuilder);
        }

        private static IDataReader ApplyEntityJoinsToReader(ORMEntity entity, IDataReader reader, SQLBuilder sqlBuilder)
        {
            foreach (var join in sqlBuilder.Joins)
            {
                if (join.IsManyToMany)
                {
                    if (join.LeftPropertyInfo.ReflectedType == entity.GetType())
                    {
                        foreach (var property in entity.GetType().GetProperties())
                        {
                            if (property.PropertyType == join.RightTableAttribute.CollectionTypeRight
                             && property.CustomAttributes.Any(x => x.AttributeType == typeof(ORMManyToMany)))
                            {
                                var parentDataTable = new DataTable();
                                parentDataTable.Load(reader);

                                var childTableName = join.RightTableAttribute.EntityType.Name;
                                var childEntity = Activator.CreateInstance(join.RightTableAttribute.EntityType, true) as ORMEntity;
                                var childCollection = Activator.CreateInstance(join.RightTableAttribute.CollectionType) as IEnumerable<ORMEntity>;

                                foreach (var childProperty in childEntity.GetType().GetProperties(childEntity.PublicFlags))
                                {
                                    var executedQuery = (string)childCollection.GetType().GetProperty(nameof(ORMCollection<ORMEntity>.ExecutedQuery)).GetValue(childCollection);

                                    if (!string.IsNullOrEmpty(executedQuery))
                                        break;

#pragma warning disable IDE0019 // Use pattern matching
                                    var fkAttribute = childProperty.GetCustomAttributes(typeof(ORMForeignKeyAttribute), false).FirstOrDefault() as ORMForeignKeyAttribute;
#pragma warning restore IDE0019 // Use pattern matching

                                    if (fkAttribute != null && fkAttribute.Relation == entity.GetType())
                                    {
                                        BinaryExpression whereExpression = null;

                                        for (int i = 0; i < childEntity.PrimaryKey.Count; i++)
                                        {
                                            if (!childEntity.IsForeignKeyOfType(childEntity.PrimaryKey.Keys[i].PropertyName, entity.GetType()))
                                                continue;

                                            // Contains the id represented as a MemberExpression: {x.InternalPropertyName}.
                                            var memberExpression = Expression.Property(Expression.Parameter(childEntity.GetType(), $"x"), childEntity.GetPrimaryKeyPropertyInfo()[i]);

                                            // Contains the actual id represented as a ConstantExpression: {id_value}.
                                            var constantExpression = Expression.Constant(entity.PrimaryKey.Keys[i].Value, entity.PrimaryKey.Keys[i].Value.GetType());

                                            // Combines the expressions represtend as a Expression: {(x.InternalPropertyName == id_value)}
                                            if (whereExpression == null)
                                                whereExpression = Expression.Equal(memberExpression, constantExpression);
                                        }

                                        // Sets the InternalWhere with the WhereExpression.
                                        childCollection.GetType().GetMethod(nameof(ORMCollection<ORMEntity>.InternalWhere), entity.NonPublicFlags, null, new Type[] { typeof(BinaryExpression) }, null).Invoke(childCollection, new object[] { whereExpression });

                                        // Fetches the data.
                                        childCollection.GetType().GetMethod(nameof(ORMCollection<ORMEntity>.Fetch), childEntity.NonPublicFlags, null, new Type[] { typeof(ORMEntity), typeof(long), typeof(Expression) }, null).Invoke(childCollection, new object[] { null, -1, null });
                                    }
                                }

                                var childCollectionRight = Activator.CreateInstance(join.RightTableAttribute.CollectionTypeRight) as IEnumerable<ORMEntity>;
                                var childEntityRight = Activator.CreateInstance(ORMUtilities.CollectionEntityRelations[childCollectionRight.GetType()]) as ORMEntity;

                                BinaryExpression whereExpressionRight = null;

                                foreach (var childEntityLeft in childCollection)
                                {
                                    for (int i = 0; i < childEntityLeft.PrimaryKey.Count; i++)
                                    {
                                        if (!childEntity.IsForeignKeyOfType(childEntityLeft.PrimaryKey.Keys[i].PropertyName, childEntityRight.GetType()))
                                            continue;

                                        // Contains the id represented as a MemberExpression: {x.InternalPropertyName}.
                                        var memberExpressionRight = Expression.Property(Expression.Parameter(childEntityRight.GetType(), $"x"), childEntityRight.GetPrimaryKeyPropertyInfo()[0]);

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
                                childCollection = Activator.CreateInstance(join.RightTableAttribute.CollectionTypeRight) as IEnumerable<ORMEntity>;

                                // Sets the InternalWhere with the WhereExpression.
                                childCollection.GetType().GetMethod(nameof(ORMCollection<ORMEntity>.InternalWhere), entity.NonPublicFlags, null, new Type[] { typeof(BinaryExpression) }, null).Invoke(childCollection, new object[] { whereExpressionRight });

                                // Fetches the data.
                                childCollection.GetType().GetMethod(nameof(ORMCollection<ORMEntity>.Fetch), entity.NonPublicFlags, null, new Type[] { typeof(ORMEntity), typeof(long), typeof(Expression) }, null).Invoke(childCollection, new object[] { null, -1, null });

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
            }

            return reader;
        }
    }
}
