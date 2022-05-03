using SharpSql.Attributes;
using SharpSql.Exceptions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SharpSql;

internal class QueryMapper
{
    internal static void DataReader<CollectionType, EntityType>(CollectionType collection, IDataReader reader, QueryBuilder queryBuilder)
       where CollectionType : SharpSqlCollection<EntityType>
       where EntityType : SharpSqlEntity
    {
        if (queryBuilder?.HasManyToManyJoins == true)
        {
            PopulateManyToManyCollection<CollectionType, EntityType>(collection, reader, queryBuilder);
            return;
        }

        PopulateCollectionFromDataReader<CollectionType, EntityType>(collection, reader, queryBuilder);
    }

    internal static void DataReader<EntityType>(EntityType entity, IDataReader reader, QueryBuilder queryBuilder)
        where EntityType : SharpSqlEntity
    {
        while (reader.Read())
        {
            PopulateEntity(entity, reader, queryBuilder);
        }
    }

    internal static void PopulateEntity<EntityType>(EntityType entity, IDataReader reader, QueryBuilder queryBuilder)
        where EntityType : SharpSqlEntity
    {
        for (int i = 0; i < reader.FieldCount; i++)
        {
            SetEntityProperty(entity, reader, queryBuilder, i);
        }

        FinaliaseEntity(entity);
    }

    internal static void PopulateChildEntity(SharpSqlEntity parentEntity, SharpSqlEntity childEntity, IDataReader reader, QueryBuilder queryBuilder)
    {
        var (ReaderStartIndex, ReaderEndIndex) = CalculateJoinIndexes(parentEntity, childEntity, queryBuilder);

        for (int i = 0; i < (ReaderEndIndex - ReaderStartIndex); i++)
        {
            var value = reader.GetValue(ReaderStartIndex + i);

            var propertyName = reader.GetName(ReaderStartIndex + i);

            if (UnitTestUtilities.IsUnitTesting)
            {
                UnitTestUtilities.PopulateChildEntity(ref propertyName, ref value, childEntity);
            }

            childEntity[propertyName] = value;
        }

        childEntity.ExecutedQuery = "Initialised through parent";

        FinaliaseEntity(childEntity);
    }

    private static (int ReaderStartIndex, int ReaderEndIndex) CalculateJoinIndexes(SharpSqlEntity parentEntity, SharpSqlEntity childEntity, QueryBuilder queryBuilder)
    {
        var startIndex = 0;

        foreach (var (name, type) in queryBuilder.TableOrder)
        {
            // The TableOrder (as the name suggests) provides the order from the SqlReader
            // with all the involved tables. Therefore the parent will always be set first
            // and we can break once the current join has been found.
            if (type == parentEntity.GetType())
            {
                startIndex = queryBuilder.TableNameColumnCount[name];
            }
            // If there are multiple joins, we want to continue adding up the total tally
            // untill our current join type is found so we know the current index.
            else if (type != parentEntity.GetType()
                  && type != childEntity.GetType())
            {
                startIndex += queryBuilder.TableNameColumnCount[name];
            }
            else if (type == childEntity.GetType())
            {
                // We found the indexes based on the parent entity fields and the previous
                // joins within the queryBuilder.
                var endIndex = startIndex + queryBuilder.TableNameColumnCount[name];

                return (startIndex, endIndex);
            }
        }

        // This shouldn't happen, but leaving an exception for now to make sure all cases work as expected.
        // -Rick, 11 December 2020
        throw new NotImplementedException();
    }

    internal static void FinaliaseEntity(SharpSqlEntity entity)
    {
        entity.ObjectState = ObjectState.Fetched;

        if (!entity.DisableChangeTracking)
        {
            entity.GetType()
                  .GetProperty(nameof(SharpSqlEntity.OriginalFetchedValue), SharpSqlEntity.NonPublicFlags)
                  .SetValue(entity, entity.ShallowCopy());

            foreach (var relation in entity.Relations.Where(x => x != null && !x.IsNew))
            {
                entity.OriginalFetchedValue[relation.GetType().Name] = (entity[relation.GetType().Name] as SharpSqlEntity).OriginalFetchedValue;
            }
        }

        entity.PrimaryKey.Update(entity);

        if (UnitTestUtilities.IsUnitTesting)
        {
            // With unit tests, the SharpSqlPrimaryKey isn't always set - e.g:
            // A (child) object is spawned from within the framework, therefore the private/internal
            // constructor is called. In this case, the SharpSqlPrimaryKey value will never be set.
            // ~ Rick, 12/01/2021

            foreach (var primaryKey in entity.PrimaryKey.Keys)
            {
                primaryKey.Value = entity[primaryKey.PropertyName];
            }
        }
    }



    //SELECT U.Id, U.Username, U.Organisation, U.DateCreated, U.DateLastModified, R.Id, R.Name FROM [DBO].[USERS] AS [U]
    //LEFT JOIN[DBO].[USERROLES] AS[UU] ON [U].[ID] = [UU].[USERID]
    //LEFT JOIN [DBO].[ROLES] AS[R] ON [UU].[ROLEID] = [R].[ID] WHERE ([U].[ID] = 1);


    //SELECT U.Id, U.Username, U.Organisation, U.DateCreated, U.DateLastModified, O.Id, O.Name, R.Id, R.Name FROM [DBO].[USERS] AS [U]
    //LEFT JOIN[DBO].[Organisations] AS[O] ON [U].[ID] = [O].[Id]
    //LEFT JOIN [DBO].[USERROLES] AS[UU] ON [U].[ID] = [UU].[USERID]
    //LEFT JOIN [DBO].[ROLES] AS[R] ON [UU].[ROLEID] = [R].[ID] WHERE ([U].[ID] = 1);


    //SELECT* FROM [DBO].[USERS] AS[U]
    //LEFT JOIN[DBO].[USERROLES] AS[UU] ON[U].[ID] = [UU].[USERID]
    //LEFT JOIN[DBO].[ROLES] AS[R] ON[UU].[ROLEID] = [R].[ID] WHERE([U].[ID] = 1);


    //SELECT* FROM [DBO].[USERS] AS[U]
    //LEFT JOIN[DBO].[Organisations] AS[O] ON[U].[ID] = [O].[Id]
    //LEFT JOIN[DBO].[USERROLES] AS[UU] ON[U].[ID] = [UU].[USERID]
    //LEFT JOIN[DBO].[ROLES] AS[R] ON[UU].[ROLEID] = [R].[ID] WHERE([U].[ID] = 1);

    // ALSO make unit tests for INNER variants!!!!!!!!!!!

    private static DataTable CopyDataTableSection(DataTable rootDataTable, ref int tableIndex, QueryBuilder queryBuilder, ref int tableOrderIndex)
    {
        var dataTable = new DataTable();
        dataTable.ExtendedProperties.Add(Constants.IsManyToMany, false);
        dataTable.ExtendedProperties.Add(Constants.ManyToMany, new List<(Type Left, Type Right)>(4)); //@Performance: calculate capacity based on many-to-many
        var tableName = queryBuilder.TableOrder[tableOrderIndex].Name;
        var tableType = queryBuilder.TableOrder[tableOrderIndex].Type;
        var tableColumnCount = queryBuilder.TableNameColumnCount[tableName];

        dataTable.TableName = SharpSqlCache.CollectionEntityRelations[tableType].Name;

        ProcessManyToManySection(dataTable, queryBuilder, ref tableOrderIndex, tableType, out Type m2mTableType);

        var removeIndices = new HashSet<int>(queryBuilder.AllJoins.Count);
        
        // @Memory
        var cachedColumns = SharpSqlCache.EntityColumns[tableType].Keys.ToArray();

        // Since rootDataTable is read-only (at least, it should be) we can safely use the indexes without losing/overriding data.
        for (int i = tableIndex; i < tableColumnCount + tableIndex; i++)
        {
            var cacheIndex = i - tableIndex;
            var columnName = cachedColumns[cacheIndex];

            if (UnitTestUtilities.IsUnitTesting && columnName.Contains('_'))
            {
                columnName = columnName.Split('_').Last();
            }
  
            if (SharpSqlCache.EntityColumns[tableType][columnName] == ColumnType.ManyToMany)
            {
                if (queryBuilder.HasManyToManyJoins)
                {
                    // @Todo
                    throw new NotImplementedException();
                    // if it does have many-to-many joins, check if the current join exists, skip or add based on it.
                }

                removeIndices.Add(i);
            }
            else if (SharpSqlCache.EntityColumns[tableType][columnName] == ColumnType.Join)
            {
                if (queryBuilder.HasJoins)
                {
                    // @Todo
                    throw new NotImplementedException();
                    // if it does have joins, check if the current join exists, skip or add based on it.
                }
                // ManyToMany joins aren't all many-to-many joins, because the right side is a regular table.
                // We need to check if the current table isn't actually a companion of a many-to-many.
                else if (queryBuilder.HasManyToManyJoins)
                {
                    // If one is found, it's actually a many-to-many.
                    if (queryBuilder.AllJoins.Where(x => x.LeftTableAttribute.EntityType == tableType && x.IsManyToMany == false).Any()
                     || SharpSqlCache.ManyToMany.ContainsKey(tableType))
                    {
                        dataTable.Columns.Add(columnName, rootDataTable.Columns[i].DataType);
                        continue;
                    }
                }

                removeIndices.Add(i);
            }

            dataTable.Columns.Add(columnName, rootDataTable.Columns[i].DataType);
        }

        var isTableTypeRight = m2mTableType == null && 
            SharpSqlCache.ManyToManyRelations.Any(x => x.Value.CollectionTypeRight == SharpSqlCache.CollectionEntityRelations[tableType]);

        for (int i = 0; i < rootDataTable.Rows.Count; i++)
        {
            if (i > 0 && tableType != null
            || (i > 0 && m2mTableType != null && SharpSqlCache.ManyToMany.ContainsKey(m2mTableType)))
            {
                // We check if the current rows equals the previous rows, because we only want to add duplicates
                // if its a many-to-many record.
                if (Enumerable.SequenceEqual(rootDataTable.Rows[i - 1].ItemArray[tableIndex..(tableColumnCount + tableIndex)],
                                             rootDataTable.Rows[i].ItemArray[tableIndex..(tableColumnCount + tableIndex)]))
                {
                    continue;
                }
                else if (isTableTypeRight && ContainsDataTableEntry(dataTable, rootDataTable.Rows[i].ItemArray[tableIndex..(tableColumnCount + tableIndex)]))
                {
                    continue;
                }
                // When all results are all null, skip it.
                else if (rootDataTable.Rows[i].ItemArray[tableIndex..(tableColumnCount + tableIndex)].All(x => x == DBNull.Value))
                {
                    continue;
                }
            }

            dataTable.Rows.Add(rootDataTable.Rows[i].ItemArray[tableIndex..(tableColumnCount + tableIndex)]);
        }

        tableIndex += tableColumnCount;

        // Maybe someday we can think of something nicer ¯\_(ツ)_/¯
        foreach (var removeIndice in removeIndices)
        {
            dataTable.Columns.RemoveAt(removeIndice);
        }

        return dataTable;
    }

    private static bool ContainsDataTableEntry(DataTable dataTable, object[] values)
    {
        for (int i = 0; i < dataTable.Rows.Count; i++)
        {
            if (Enumerable.SequenceEqual(values, dataTable.Rows[i].ItemArray))
                return true;
        }
        return false;
    }

    private static void ProcessManyToManySection(DataTable dataTable, QueryBuilder queryBuilder, ref int tableOrderIndex, Type tableType, out Type m2mTableType)
    {
        m2mTableType = null;

        // Misschien versimpeling door te kijken naar tableType in cache?
        foreach (var m2m in SharpSqlCache.ManyToManyRelations)
        {
            if (m2m.Value.EntityType == tableType)
            {
                var tableOrderLeft = queryBuilder.TableOrder[tableOrderIndex];
                var tableOrderRight = queryBuilder.TableOrder[tableOrderIndex + 1];
                var m2mColumnCount = queryBuilder.TableNameColumnCount[tableOrderLeft.Name];

                m2mTableType = tableOrderLeft.Type;

                dataTable.ExtendedProperties[Constants.IsManyToMany] = true;
                ((List<(Type, Type)>)dataTable.ExtendedProperties[Constants.ManyToMany])
                    .Add((SharpSqlCache.CollectionEntityRelations[tableOrderLeft.Type],
                          SharpSqlCache.CollectionEntityRelations[tableOrderRight.Type]));

                break;
            }
        }
    }

    private static void PopulateManyToManyCollection<CollectionType, EntityType>(CollectionType collection, IDataReader reader, QueryBuilder queryBuilder)
        where CollectionType : SharpSqlCollection<EntityType>
        where EntityType : SharpSqlEntity
    {
        // Immutable!
        var parentDataTable = new DataTable();
        parentDataTable.Load(reader);

        // @Todo: count all joins with ManyToMany true and divide by 2 and substract that from TableOrder.Count.
        var dataTables = new List<DataTable>(queryBuilder.TableOrder.Count);

        var tableIndex = 0;
        for (int tableOrderIndex = 0; tableOrderIndex < queryBuilder.TableOrder.Count; tableOrderIndex++)
        {
            dataTables.Add(CopyDataTableSection(parentDataTable, ref tableIndex, queryBuilder, ref tableOrderIndex));
        }

        reader.Dispose();

        for (int i = 0; i < dataTables.Count; i++)
        {
            // ManyToMany relation
            if ((bool)dataTables[i].ExtendedProperties[Constants.IsManyToMany])
            {
                var manyToManyRelations = (List<(Type Left, Type Right)>)dataTables[i].ExtendedProperties[Constants.ManyToMany];
                var entityManyToManyProperties = new List<(Type Left, PropertyInfo Property)>(manyToManyRelations.Count);

                foreach (EntityType entity in collection)
                {
                    if (entityManyToManyProperties.Count != manyToManyRelations.Count)
                    {
                        foreach ((Type Left, Type Right) in manyToManyRelations)
                        {
                            var properties = entity.GetType().GetProperties().Where(x => x.PropertyType == Right && x.CustomAttributes.Any(x => x.AttributeType == typeof(SharpSqlManyToManyAttribute)));

                            if (!properties.Any())
                            {
                                // Somebody is trying to spawn a many-to-many collection without an existing property on the entity?
                                throw new IllegalColumnNameException($"The entity of type {entity.GetType().Name} does not have a property for the many-to-many relation of type {Right.Name}.");
                            }
                            else if (properties.Count() > 1)
                            {
                                throw new InvalidJoinException($"Multiple properties found with the attribute {typeof(SharpSqlManyToManyAttribute).Name} for type {Right.Name}.");
                            }

                            entityManyToManyProperties.Add((Left, properties.FirstOrDefault()));
                        }
                    }

                    foreach (var manyToManyProperty in entityManyToManyProperties)
                    {
                        // we only need property from manyToManyProperty
                        SetManyToManyProperty(entity, dataTables[i], dataTables[i + 1], manyToManyProperty);
                    }
                }
                // When a many-to-many section is complete, the next table in the list can be skipped since it has been processed.
                i++;
            }
            // Default columns
            else
            {
                PopulateCollectionFromDataReader<CollectionType, EntityType>(collection, dataTables[i].CreateDataReader(), queryBuilder);
            }
        }
    }

    private static void PopulateCollectionFromDataReader<CollectionType, EntityType>(CollectionType collection, IDataReader reader, QueryBuilder queryBuilder)
        where CollectionType : SharpSqlCollection<EntityType>
        where EntityType : SharpSqlEntity
    {
        while (reader.Read())
        {
            var entity = (EntityType)Activator.CreateInstance(typeof(EntityType), true);
            entity.ExecutedQuery = "Initialised through collection";
            entity.DisableChangeTracking = collection.DisableChangeTracking;

            PopulateEntity(entity, reader, queryBuilder);

            collection.Add(entity);
 
        }

        reader.Dispose();
    }

    private static void PopulateManyToManyCollectionFromDataReader(object collection, Type entityType, IDataReader reader, bool disableChangeTracking)
    {
        while (reader.Read())
        {
            var entity = Activator.CreateInstance(entityType, true) as SharpSqlEntity;
            entity.DisableChangeTracking = disableChangeTracking;
            entity.ExecutedQuery = "Initialised through collection";

            PopulateEntity(entity, reader, null);

            collection.GetType().GetMethod(nameof(SharpSqlCollection<SharpSqlEntity>.Add), SharpSqlEntity.PublicFlags).Invoke(collection, new object[] { entity });
            collection.GetType().GetProperty(nameof(SharpSqlCollection<SharpSqlEntity>.ExecutedQuery), SharpSqlEntity.PublicFlags).SetValue(collection, "Initialised through parent");
        }

        reader.Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void SetManyToManyProperty(SharpSqlEntity entity, DataTable dataTableLeft, DataTable dataTableRight, (Type Left, PropertyInfo Property) manyToManyProperty)
    {
        var attribute = manyToManyProperty.Left.GetCustomAttribute<SharpSqlTableAttribute>();
        var foreignKeyProperties = attribute.EntityType.GetProperties().Where(property => property.IsDefined(typeof(SharpSqlForeignKeyAttribute), false));

        foreach (var property in foreignKeyProperties)
        {
            if (property.GetCustomAttribute<SharpSqlForeignKeyAttribute>()?.Relation == entity.GetType())
            {
                var collection = Activator.CreateInstance(manyToManyProperty.Property.PropertyType);

                GetManyToManyEntityReader(entity, collection, attribute, dataTableLeft, dataTableRight, manyToManyProperty);

                if ((collection as IEnumerable<SharpSqlEntity>).Any())
                {
                    entity[manyToManyProperty.Property.Name] = collection;
                }
            }
        }
    }

    // @todo Rename this
    internal static void GetManyToManyEntityReader(SharpSqlEntity entity, object entityCollection, SharpSqlTableAttribute manyToManyTableAttribute, DataTable dataTableLeft, DataTable dataTableRight, (Type Left, PropertyInfo Property) manyToManyProperty)
    {
        DataTable correctData = dataTableRight.Clone();

        // create dataReader from dataTableLeft and dataTableRight based on entity.PrimaryKey and pass it to PopulateChildEntity
        for (int i = 0; i < dataTableLeft.Rows.Count; i++)
        {
            if (entity.PrimaryKey.IsCombinedPrimaryKey)
                throw new NotImplementedException();

            // @Todo: implement proper multiple primary key usage, and refactor primary key on entity level.
            var leftPk = SharpSqlCache.PrimaryKeys[SharpSqlCache.CollectionEntityRelations[manyToManyTableAttribute.CollectionTypeLeft]].Keys[0];
            var m2mPkLeft = SharpSqlCache.PrimaryKeys[SharpSqlCache.CollectionEntityRelations[manyToManyTableAttribute.CollectionType]].Keys[0];
            var m2mPkRight = SharpSqlCache.PrimaryKeys[SharpSqlCache.CollectionEntityRelations[manyToManyTableAttribute.CollectionType]].Keys[1];
            var rightPk = SharpSqlCache.PrimaryKeys[SharpSqlCache.CollectionEntityRelations[manyToManyTableAttribute.CollectionTypeRight]].Keys[0];

            foreach (DataRow rightRow in dataTableRight.Rows)
            {
                if (UnitTestUtilities.IsUnitTesting)
                {
                    var left = dataTableLeft.Rows[i][m2mPkLeft.ColumnName];

                    // Unit tests columns are all of type string, therefore they require to be converted to their respective type.
                    UnitTestUtilities.ChangeDataTableType(Activator.CreateInstance(manyToManyTableAttribute.EntityType, true) as SharpSqlEntity, m2mPkLeft.ColumnName, ref left);

                    if (rightRow[rightPk.ColumnName].Equals(dataTableLeft.Rows[i][m2mPkRight.ColumnName])
                     && left.Equals(entity[leftPk.ColumnName]))
                    {
                        correctData.ImportRow(rightRow);
                    }
                }
                else if (rightRow[rightPk.ColumnName].Equals(dataTableLeft.Rows[i][m2mPkRight.ColumnName])
                      && dataTableLeft.Rows[i][m2mPkLeft.ColumnName].Equals(entity[leftPk.ColumnName]))
                {
                    correctData.ImportRow(rightRow);
                }
            }
        }

        PopulateManyToManyCollectionFromDataReader(entityCollection, SharpSqlCache.CollectionEntityRelations[manyToManyProperty.Property.PropertyType], correctData.CreateDataReader(), entity.DisableChangeTracking);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void SetEntityProperty(SharpSqlEntity entity, IDataReader reader, QueryBuilder queryBuilder, int iteration)
    {
        // All joins (child-entities) are filled through PopulateChildEntity, therefore we can
        // skip anything past the current entity (parent) within the reader.
        // When executing DirectQueries the queryBuilder is null, and since joins aren't supported
        // in DirectQueries, this can be ignored safely.
        if (iteration >= queryBuilder?.TableNameColumnCount.First().Value)
        {
            // Skipping.
            return;
        }

        var columnName = reader.GetName(iteration);

        if (UnitTestUtilities.IsUnitTesting)
        {
            columnName = columnName.Split('_').Last();
        }

        var entityPropertyInfo = entity.GetPropertyInfo(columnName);

        if (null == entityPropertyInfo)
        {
            if (columnName == entity.GetType().Name)
            {
                throw new IllegalColumnNameException($"The column [{columnName}] has not been implemented in entity [{entity.GetType().Name}], but can't have the same name as its enclosing type.");
            }

            throw new NotImplementedException($"The column [{columnName}] has not been implemented in entity [{entity.GetType().Name}].");
        }
        else if (!entityPropertyInfo.CanWrite)
        {
            throw new ReadOnlyException($"Property [{columnName}] is read-only in [{entity.GetType().Name}].");
        }

        object value = null;

        switch (entityPropertyInfo.PropertyType)
        {
            case Type type when type == typeof(DateTime?):
                value = reader.GetValue(iteration);
                break;
            case Type type when type == typeof(DateTime):
                if (reader.GetValue(iteration) == DBNull.Value)
                {
                    throw new PropertyNotNullableException($"Property [{columnName}] is not nullable, but the database column equivelant is.");
                }

                value = reader.GetValue(iteration);
                break;
            case Type type when type.IsSubclassOf(typeof(SharpSqlEntity)):
                if (reader.GetValue(iteration) == DBNull.Value)
                {
                    break;
                }
                // If there are no joins provided or none matched the current type we don't want
                // to fetch the child-object.
                if (queryBuilder.AllJoins.Count == 0 || !queryBuilder.AllJoins.Any(x => x.LeftPropertyInfo.PropertyType == type))
                {
                    value = null;
                    break;
                }

                foreach (var join in queryBuilder.AllJoins)
                {
                    if (join.LeftPropertyInfo.PropertyType == type)
                    {
                        var subEntity = Activator.CreateInstance(type.UnderlyingSystemType) as SharpSqlEntity;

                        PopulateChildEntity(entity, subEntity, reader, queryBuilder);

                        value = subEntity;

                        entity.Relations.Add(value as SharpSqlEntity);
                        break;
                    }
                }

                break;
            default:
                value = reader.GetValue(iteration);
                break;
        }

        // Unit tests columns are all of type string, therefore they require to be converted to their respective type.
        UnitTestUtilities.ChangeDataTableType(entityPropertyInfo, ref value);

        if (reader.GetValue(iteration) == DBNull.Value)
        {
            entityPropertyInfo.SetValue(entity, null);
        }
        else
        {
            entityPropertyInfo.SetValue(entity, value);
        }
    }
}