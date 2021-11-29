using SharpSql.Exceptions;
using SharpSql.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SharpSql
{
    internal class QueryMapper
    {
        internal static void DataReader<CollectionType, EntityType>(CollectionType collection, IDataReader reader, QueryBuilder queryBuilder)
           where CollectionType : SharpSqlCollection<EntityType>
           where EntityType : SharpSqlEntity
        {
            if (queryBuilder?.ContainsToManyJoins == true)
            {
                PopulateManyToManyCollection<CollectionType, EntityType>(collection, reader, queryBuilder);
                return;
            }

            while (reader.Read())
            {
                var entity = (EntityType)Activator.CreateInstance(typeof(EntityType), true);
                entity.DisableChangeTracking = collection.DisableChangeTracking;

                PopulateEntity(entity, reader, queryBuilder);

                entity.ExecutedQuery = "Initialised through collection";

                collection.Add(entity);
            }
        }

        private static readonly Dictionary<SharpSqlPrimaryKey, bool> PrimaryKeyCache = new(8);

        internal static void DataReader<EntityType>(EntityType entity, IDataReader reader, QueryBuilder queryBuilder)
            where EntityType : SharpSqlEntity
        {
            while (reader.Read())
            {
                PopulateEntity(entity, reader, queryBuilder);

                if (!UnitTestUtilities.IsUnitTesting)
                {
                    PopulateManyToManyEntity(entity, reader, queryBuilder);
                }
                if (!PrimaryKeyCache.ContainsKey(entity.PrimaryKey))
                {
                    PrimaryKeyCache.Add(entity.PrimaryKey, true);
                }
            }

            if (PrimaryKeyCache.ContainsKey(entity.PrimaryKey))
            {
                PrimaryKeyCache.Remove(entity.PrimaryKey);
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

        private static void PopulateManyToManyCollection<CollectionType, EntityType>(CollectionType collection, IDataReader reader, QueryBuilder queryBuilder)
            where CollectionType : SharpSqlCollection<EntityType>
            where EntityType : SharpSqlEntity
        {
            Dictionary<SharpSqlPrimaryKey, Dictionary<string, List<SharpSqlEntity>>> manyToManyData = new(new SharpSqlPrimaryKey());
            Dictionary<SharpSqlPrimaryKey, EntityType> knownEntities = new(new SharpSqlPrimaryKey());

            var manyToManyJoinIndexes = new List<(string, int[])>();
            var manyToManyJoinTypes = new Dictionary<string, Type>();

            var tableIndex = 0;
            foreach (var (name, tableType) in queryBuilder.TableOrder)
            {
                var objectPath = queryBuilder.TableNameResolvePaths.ContainsKey(name) ? queryBuilder.TableNameResolvePaths[name] : string.Empty;
                var tableColumnCount = queryBuilder.TableNameColumnCount[name];

                foreach (var join in queryBuilder.Joins)
                {
                    if (join.LeftTableAttribute.EntityType == tableType && join.IsManyToMany)
                    {
                        var indexes = new List<int>();
                        for (int i = 0; i < tableColumnCount; i++)
                        {
                            indexes.Add(tableIndex + i);
                        }
                        manyToManyJoinIndexes.Add((join.LeftPropertyInfo.Name, indexes.ToArray()));

                    }
                }

                tableIndex += tableColumnCount;
            }

            void AddManyToManyObject(SharpSqlPrimaryKey key, IDataReader reader)
            {
                Dictionary<string, List<SharpSqlEntity>> relations;
                if (manyToManyData.ContainsKey(key))
                {
                    relations = manyToManyData[key];
                }
                else
                {
                    relations = new Dictionary<string, List<SharpSqlEntity>>();
                    manyToManyData[key] = relations;
                }

                foreach (var (fieldName, indexes) in manyToManyJoinIndexes)
                {
                    bool IsRowEmpty(List<(string, int[])> manyToManyJoinIndexes, IDataReader reader)
                    {
                        foreach (var (fieldName, indexes) in manyToManyJoinIndexes)
                        {
                            for (int i = 0; i < indexes.Length; i++)
                            {
                                if (reader.GetValue(indexes[i]) == DBNull.Value)
                                {
                                    if ((i + 1) == indexes.Length)
                                    {
                                        return true;
                                    }
                                }
                                else
                                {
                                    return false;
                                }
                            }
                        }

                        return true;
                    }

                    if (!IsRowEmpty(manyToManyJoinIndexes, reader))
                    {
                        var instance = (SharpSqlEntity)Activator.CreateInstance(manyToManyJoinTypes[fieldName]);
                        foreach (var index in indexes)
                        {
                            SetEntityProperty(instance, reader, queryBuilder, index, true);
                        }

                        if (relations.ContainsKey(fieldName))
                        {
                            relations[fieldName].Add(instance);
                        }
                        else
                        {
                            relations[fieldName] = new List<SharpSqlEntity>()
                        {
                            instance
                        };
                        }
                    }
                }
            }

            int[] primaryKeyIndexes = null;

            bool isFirst = true;
            while (reader.Read())
            {
                SharpSqlPrimaryKey pk = default;
                if (!isFirst)
                {
                    pk = new SharpSqlPrimaryKey(reader, primaryKeyIndexes);
                    if (knownEntities.ContainsKey(pk))
                    {
                        // Only do to many linking here
                        AddManyToManyObject(pk, reader);
                        continue;
                    }
                }

                var entity = (EntityType)Activator.CreateInstance(typeof(EntityType));
                entity.DisableChangeTracking = collection.DisableChangeTracking;

                if (isFirst)
                {
                    primaryKeyIndexes = SharpSqlPrimaryKey.DeterminePrimaryKeyIndexes(reader, entity);

                    foreach (var (fieldName, _) in manyToManyJoinIndexes)
                    {
                        var type = entity.GetType().GetProperty(fieldName, SharpSqlEntity.PublicFlags).PropertyType;
                        if (typeof(SharpSqlEntity).IsAssignableFrom(type.GetType()))
                        {
                            type = SharpSqlUtilities.CollectionEntityRelations[type];
                        }
                        manyToManyJoinTypes.Add(fieldName, type);
                    }

                    pk = new SharpSqlPrimaryKey(reader, primaryKeyIndexes);
                    isFirst = false;
                }

                PopulateEntity(entity, reader, queryBuilder);
                AddManyToManyObject(pk, reader);

                knownEntities.Add(pk, entity);
            }

            foreach (var kvPair in manyToManyData)
            {
                var entity = knownEntities[kvPair.Key];
                foreach (var data in kvPair.Value)
                {
                    var property = entity.GetType().GetProperty(data.Key, SharpSqlEntity.PublicFlags);
                    if (typeof(ISharpSqlCollection<EntityType>).IsAssignableFrom(property.PropertyType))
                    {
                        var subcollection = Activator.CreateInstance(property.PropertyType);
                        var collectionProperty = property.PropertyType.GetProperty(nameof(SharpSqlCollection<SharpSqlEntity>.MutableEntityCollection), SharpSqlEntity.NonPublicFlags);
                        var list = collectionProperty.GetValue(subcollection) as IList;
                        foreach (var item in data.Value)
                        {
                            list.Add(item);
                        }
                        property.SetValue(entity, subcollection);
                    }
                    else
                    {
                        throw new Exception("Something went wrong trying to cast to a subcollection");
                    }
                }

                collection.Add(entity);
            }
        }

        private static void PopulateManyToManyEntity(SharpSqlEntity entity, IDataReader reader, QueryBuilder queryBuilder)
        {
            Dictionary<SharpSqlPrimaryKey, Dictionary<string, List<SharpSqlEntity>>> manyToManyData = new(new SharpSqlPrimaryKey());

            var manyToManyJoinIndexes = new List<(string, int[])>();
            var manyToManyJoinTypes = new Dictionary<string, Type>();

            var tableIndex = 0;
            foreach (var (name, _) in queryBuilder.TableOrder)
            {
                var objectPath = queryBuilder.TableNameResolvePaths.ContainsKey(name) ? queryBuilder.TableNameResolvePaths[name] : string.Empty;
                var tableColumnCount = queryBuilder.TableNameColumnCount[name];
                foreach (var join in queryBuilder.Joins)
                {
                    if (join.IsManyToMany)
                    {
                        var indexes = new List<int>();
                        for (int i = 0; i < tableColumnCount; i++)
                        {
                            indexes.Add(tableIndex + i);
                        }
                        manyToManyJoinIndexes.Add((objectPath.Split('.')[1], indexes.ToArray()));
                    }
                }
                tableIndex += tableColumnCount;
            }

            void AddManyToManyObject(SharpSqlPrimaryKey key, IDataReader reader)
            {
                Dictionary<string, List<SharpSqlEntity>> relations;
                if (manyToManyData.ContainsKey(key))
                {
                    relations = manyToManyData[key];
                }
                else
                {
                    relations = new Dictionary<string, List<SharpSqlEntity>>();
                    manyToManyData[key] = relations;
                }

                foreach (var (fieldName, indexes) in manyToManyJoinIndexes)
                {
                    bool IsRowEmpty(List<(string, int[])> manyToManyJoinIndexes, IDataReader reader)
                    {
                        foreach (var (fieldName, indexes) in manyToManyJoinIndexes)
                        {
                            for (int i = 0; i < indexes.Length; i++)
                            {
                                if (reader.GetValue(indexes[i]) == DBNull.Value)
                                {
                                    if ((i + 1) == indexes.Length)
                                    {
                                        return true;
                                    }
                                }
                                else
                                {
                                    return false;
                                }
                            }
                        }

                        return true;
                    }

                    if (!IsRowEmpty(manyToManyJoinIndexes, reader))
                    {
                        var instance = (SharpSqlEntity)Activator.CreateInstance(manyToManyJoinTypes[fieldName]);
                        foreach (var index in indexes)
                        {
                            SetEntityProperty(instance, reader, queryBuilder, index, true);
                        }

                        if (relations.ContainsKey(fieldName))
                        {
                            relations[fieldName].Add(instance);
                        }
                        else
                        {
                            relations[fieldName] = new List<SharpSqlEntity>()
                        {
                            instance
                        };
                        }
                    }
                }
            }

            int[] primaryKeyIndexes = SharpSqlPrimaryKey.DeterminePrimaryKeyIndexes(reader, entity);

            foreach (var (fieldName, _) in manyToManyJoinIndexes)
            {
                var type = entity.GetPropertyInfo(fieldName).PropertyType;
                if (!typeof(SharpSqlEntity).IsAssignableFrom(type.GetType()))
                {
                    type = SharpSqlUtilities.CollectionEntityRelations[type];
                }
                manyToManyJoinTypes.Add(fieldName, type);
            }

            SharpSqlPrimaryKey pk = new(reader, primaryKeyIndexes);

            AddManyToManyObject(pk, reader);

            foreach (var kvPair in manyToManyData)
            {
                foreach (var data in kvPair.Value)
                {
                    var property = entity.GetType().GetProperty(data.Key, SharpSqlEntity.PublicFlags);
                    if (typeof(ISharpSqlCollection<SharpSqlEntity>).IsAssignableFrom(property.PropertyType))
                    {
                        var propertyValue = entity.GetType().GetProperty(data.Key, SharpSqlEntity.PublicFlags).GetValue(entity);

                        if (propertyValue == null)
                        {
                            var subcollection = Activator.CreateInstance(property.PropertyType);

                            var collectionProperty = property.PropertyType.GetProperty(nameof(SharpSqlCollection<SharpSqlEntity>.MutableEntityCollection), SharpSqlEntity.NonPublicFlags);
                            var list = collectionProperty.GetValue(subcollection) as IList;
                            foreach (var item in data.Value)
                            {
                                list.Add(item);
                            }
                            property.SetValue(entity, subcollection);
                        }
                        else
                        {
                            foreach (var item in data.Value)
                            {
                                propertyValue.GetType().GetMethod(nameof(IList.Add), SharpSqlEntity.PublicFlags).Invoke(propertyValue, new object[] { item });
                            }
                        }
                    }
                    else
                    {
                        throw new Exception("Something went wrong trying to cast to a subcollection");
                    }
                }
            }
        }





        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetEntityProperty(SharpSqlEntity entity, IDataReader reader, QueryBuilder queryBuilder, int iteration, bool isEntityManyTomany = false)
        {
            // All joins (child-entities) are filled through PopulateChildEntity, therefore we can
            // skip anything past the current entity (parent) within the reader.
            // When executing DirectQueries the queryBuilder is null, and since joins aren't supported
            // in DirectQueries, this can be ignored safely.
            if (iteration >= queryBuilder?.TableNameColumnCount.First().Value && !isEntityManyTomany)
            {
                // Skipping.
                return;
            }

            var propertyName = reader.GetName(iteration);

            if (UnitTestUtilities.IsUnitTesting)
            {
                propertyName = propertyName.Split('_').Last();
            }

            var entityPropertyInfo = entity.GetPropertyInfo(propertyName);

            if (null == entityPropertyInfo)
            {
                if (propertyName == entity.GetType().Name)
                {
                    throw new IllegalColumnNameException($"The column [{propertyName}] has not been implemented in entity [{entity.GetType().Name}], but can't have the same name as its enclosing type.");
                }

                throw new NotImplementedException($"The column [{propertyName}] has not been implemented in entity [{entity.GetType().Name}].");
            }
            else if (!entityPropertyInfo.CanWrite)
            {
                throw new ReadOnlyException($"Property [{propertyName}] is read-only in [{entity.GetType().Name}].");
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
                        throw new PropertyNotNullableException($"Property [{propertyName}] is not nullable, but the database column equivelant is.");
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
                    if (queryBuilder.Joins.Count == 0 || !queryBuilder.Joins.Any(x => x.LeftPropertyInfo.PropertyType == type))
                    {
                        value = null;
                        break;
                    }

                    foreach (var join in queryBuilder.Joins)
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

            if (UnitTestUtilities.IsUnitTesting)
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
}
