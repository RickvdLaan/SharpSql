using ORM.Attributes;
using ORM.Exceptions;
using ORM.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ORM
{
    internal class SQLHelper
    {
        internal static void DataReader<CollectionType, EntityType>(CollectionType collection, IDataReader reader, SQLBuilder sqlBuilder)
           where CollectionType : ORMCollection<EntityType>
           where EntityType : ORMEntity
        {
            if (sqlBuilder?.ContainsToManyJoins == true)
            {
                PopulateCollection<CollectionType, EntityType>(collection, reader, sqlBuilder);
                return;
            }

            while (reader.Read())
            {
                var entity = (EntityType)Activator.CreateInstance(typeof(EntityType));
                entity.DisableChangeTracking = collection.DisableChangeTracking;

                PopulateEntity(entity, reader, sqlBuilder);

                collection.Add(entity);
            }
        }

        internal static void DataReader<EntityType>(EntityType entity, IDataReader reader, SQLBuilder sqlBuilder)
            where EntityType : ORMEntity
        {
            while (reader.Read())
            {
                PopulateEntity(entity, reader, sqlBuilder);
            }
        }

        internal static void PopulateEntity<EntityType>(EntityType entity, IDataReader reader, SQLBuilder sqlBuilder)
            where EntityType : ORMEntity
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                SetEntityProperty(entity, reader, sqlBuilder, i);
            }

            FinaliaseEntity(entity);
        }

        internal static void PopulateChildEntity(ORMEntity parentEntity, ORMEntity childEntity, IDataReader reader, SQLBuilder sqlBuilder)
        {
            var (ReaderStartIndex, ReaderEndIndex) = CalculateJoinIndexes(parentEntity, childEntity, sqlBuilder);

            for (int i = 0; i < (ReaderEndIndex - ReaderStartIndex); i++)
            {
                var value = reader.GetValue(ReaderStartIndex + i);

                var propertyName = reader.GetName(ReaderStartIndex + i);

#if DEBUG
                if (ORMUtilities.IsUnitTesting)
                {
                    propertyName = propertyName.Split('_').Last();

                    var childPropertyType = childEntity.GetType().GetProperty(propertyName).PropertyType;

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
#endif

                childEntity[propertyName] = value;
            }

            childEntity.ExecutedQuery = "Initialised through parent";

            FinaliaseEntity(childEntity);
        }

        private static (int ReaderStartIndex, int ReaderEndIndex) CalculateJoinIndexes(ORMEntity parentEntity, ORMEntity childEntity, SQLBuilder sqlBuilder)
        {
            var startIndex = 0;

            foreach (var (name, type) in sqlBuilder.TableOrder)
            {
                // The TableOrder (as the name suggests) provides the order from the SqlReader
                // with all the involved tables. Therefore the parent will always be set first
                // and we can break once the current join has been found.
                if (type == parentEntity.GetType())
                {
                    startIndex = sqlBuilder.TableNameColumnCount[name];
                }
                // If there are multiple joins, we want to continue adding up the total tally
                // untill our current join type is found so we know the current index.
                else if (type != parentEntity.GetType()
                      && type != childEntity.GetType())
                {
                    startIndex += sqlBuilder.TableNameColumnCount[name];
                }
                else if (type == childEntity.GetType())
                {
                    // We found the indexes based on the parent entity fields and the previous
                    // joins within the sqlBuilder.
                    var endIndex = startIndex + sqlBuilder.TableNameColumnCount[name];

                    return (startIndex, endIndex);
                }
            }

            // This shouldn't happen, but leaving an exception for now to make sure all cases work as expected.
            // -Rick, 11 December 2020
            throw new NotImplementedException();
        }

        internal static void FinaliaseEntity(ORMEntity entity)
        {
            entity.IsNew = false;

            if (!entity.DisableChangeTracking)
            {
                entity.GetType()
                      .GetProperty(nameof(ORMEntity.OriginalFetchedValue), entity.NonPublicFlags)
                      .SetValue(entity, entity.ShallowCopy());

                foreach (var relation in entity.EntityRelations.Where(x => x != null && !x.IsNew))
                {
                    entity.OriginalFetchedValue[relation.GetType().Name] = (entity[relation.GetType().Name] as ORMEntity).OriginalFetchedValue;
                }
            }
        }

        private static void PopulateCollection<CollectionType, EntityType>(CollectionType collection, IDataReader reader, SQLBuilder sqlBuilder)
            where CollectionType : ORMCollection<EntityType>
            where EntityType : ORMEntity
        {
            Dictionary<ORMPrimaryKey, Dictionary<string, List<ORMEntity>>> manyToManyData = new Dictionary<ORMPrimaryKey, Dictionary<string, List<ORMEntity>>>(new ORMPrimaryKey());
            Dictionary<ORMPrimaryKey, EntityType> knownEntities = new Dictionary<ORMPrimaryKey, EntityType>(new ORMPrimaryKey());

            var manyToManyJoinIndexes = new List<(string, int[])>();
            var manyToManyJoinTypes = new Dictionary<string, Type>();

            var tableIndex = 0;
            foreach (var (name, _) in sqlBuilder.TableOrder)
            {
                var objectPath = sqlBuilder.TableNameResolvePaths.ContainsKey(name) ? sqlBuilder.TableNameResolvePaths[name] : string.Empty;
                var tableColumnCount = sqlBuilder.TableNameColumnCount[name];

                if (objectPath.StartsWith(SQLBuilder.MANY_TO_MANY_JOIN_DATA, StringComparison.Ordinal))
                {
                    var indexes = new List<int>();
                    for (int i = 0; i < tableColumnCount; i++)
                    {
                        indexes.Add(tableIndex + i);
                    }
                    manyToManyJoinIndexes.Add((objectPath.Split('.')[1], indexes.ToArray()));
                }
                tableIndex += tableColumnCount;
            }

            void AddManyToManyObject(ORMPrimaryKey key, IDataReader _reader)
            {
                Dictionary<string, List<ORMEntity>> relations;
                if (manyToManyData.ContainsKey(key))
                {
                    relations = manyToManyData[key];
                }
                else
                {
                    relations = new Dictionary<string, List<ORMEntity>>();
                    manyToManyData[key] = relations;
                }

                foreach (var (fieldName, indexes) in manyToManyJoinIndexes)
                {
                    var instance = (ORMEntity)Activator.CreateInstance(manyToManyJoinTypes[fieldName]);
                    foreach (var index in indexes)
                    {
                        SetEntityProperty(instance, _reader, sqlBuilder, index);
                    }

                    if (relations.ContainsKey(fieldName))
                    {
                        relations[fieldName].Add(instance);
                    }
                    else
                    {
                        relations[fieldName] = new List<ORMEntity>()
                        {
                            instance
                        };
                    }
                }
            }

            int[] primaryKeyIndexes = null;

            bool isFirst = true;
            while (reader.Read())
            {
                ORMPrimaryKey pk = default;
                if (!isFirst)
                {
                    pk = new ORMPrimaryKey(reader, primaryKeyIndexes);
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
                    primaryKeyIndexes = ORMPrimaryKey.DeterminePrimaryKeyIndexes(reader, entity);

                    foreach (var (fieldName, _) in manyToManyJoinIndexes)
                    {
                        var type = entity.GetType().GetProperty(fieldName, entity.PublicFlags).PropertyType;
                        if (!typeof(ORMEntity).IsAssignableFrom(type.GetType()))
                        {
                            type = ORMUtilities.CollectionEntityRelations[type];
                        }
                        manyToManyJoinTypes.Add(fieldName, type);
                    }

                    pk = new ORMPrimaryKey(reader, primaryKeyIndexes);
                    isFirst = false;
                }

                PopulateEntity(entity, reader, sqlBuilder);
                AddManyToManyObject(pk, reader);

                knownEntities.Add(pk, entity);
            }

            foreach (var kvPair in manyToManyData)
            {
                var entity = knownEntities[kvPair.Key];
                foreach (var data in kvPair.Value)
                {
                    var property = entity.GetType().GetProperty(data.Key, entity.PublicFlags);
                    if (typeof(IORMCollection).IsAssignableFrom(property.PropertyType))
                    {
                        var subcollection = Activator.CreateInstance(property.PropertyType);
                        var collectionProperty = property.PropertyType.GetProperty(nameof(ORMCollection<ORMEntity>.EntityCollection), entity.PublicFlags);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetEntityProperty(ORMEntity entity, IDataReader reader, SQLBuilder sqlBuilder, int iteration, int tableIndex = 0)
        {
            // All joins (child-entities) are filled through PopulateChildEntity, therefore we can
            // skip anything past the current entity (parent) within the reader.
            if ((iteration + tableIndex) >= sqlBuilder.TableNameColumnCount.First().Value)
            {
                // Skipping.
                return;
            }

            var propertyName = reader.GetName(iteration + tableIndex);

#if DEBUG
            if (ORMUtilities.IsUnitTesting)
            {
                propertyName = propertyName.Split('_').Last();
            }
#endif

            var entityPropertyInfo = entity.GetType().GetProperty(propertyName, entity.PublicIgnoreCaseFlags)
                                  ?? entity.GetType().GetProperties().FirstOrDefault(x => (x.GetCustomAttributes(typeof(ORMColumnAttribute), true).FirstOrDefault() as ORMColumnAttribute)?.ColumnName == propertyName);

            if (null == entityPropertyInfo)
            {
                if (propertyName == entity.GetType().Name)
                {
                    throw new ORMIllegalColumnNameException($"The column [{propertyName}] has not been implemented in entity [{entity.GetType().Name}], but can't have the same name as its enclosing type.");
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
                    value = reader.GetValue(iteration + tableIndex);
                    break;
                case Type type when type == typeof(DateTime):
                    if (reader.GetValue(iteration + tableIndex) == DBNull.Value)
                    {
                        throw new ORMPropertyNotNullableException($"Property [{propertyName}] is not nullable, but the database column equivelant is.");
                    }

                    value = reader.GetValue(iteration + tableIndex);
                    break;
                case Type type when type.IsSubclassOf(typeof(ORMEntity)):
                    if (reader.GetValue(iteration + tableIndex) == DBNull.Value)
                    {
                        break;
                    }
                    // If there are no joins provided or none matched the current type we don't want
                    // to fetch the child-object.
                    if (sqlBuilder.Joins.Count == 0 || !sqlBuilder.Joins.Any(x => x.LeftPropertyInfo.PropertyType == type))
                    {
                        value = null;
                        break;
                    }

                    foreach (var join in sqlBuilder.Joins)
                    {
                        if (join.LeftPropertyInfo.PropertyType == type)
                        {
                            var subEntity = Activator.CreateInstance(type.UnderlyingSystemType) as ORMEntity;

                            PopulateChildEntity(entity, subEntity, reader, sqlBuilder);

                            value = subEntity;

                            entity.EntityRelations.Add(value as ORMEntity);

                            if (entityPropertyInfo == null)
                            {
                                entityPropertyInfo = entity.GetType().GetProperty("Organisation", entity.PublicIgnoreCaseFlags);
                            }

                            break;
                        }
                    }

                    break;
                default:
                    value = reader.GetValue(iteration + tableIndex);
                    break;
            }

#if DEBUG
            if (ORMUtilities.IsUnitTesting)
            {
                // Unit tests columns are all of type string, therefore they require to be converted to their respective type.
                if (Nullable.GetUnderlyingType(entityPropertyInfo.PropertyType) != null && value != DBNull.Value)
                {
                    value = Convert.ChangeType(value, Nullable.GetUnderlyingType(entityPropertyInfo.PropertyType));
                }
                else if (!entityPropertyInfo.PropertyType.IsSubclassOf(typeof(ORMEntity)) && value != DBNull.Value)
                {
                    value = Convert.ChangeType(value, entityPropertyInfo.PropertyType);
                }
            }
#endif

            if (reader.GetValue(iteration + tableIndex) == DBNull.Value)
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
