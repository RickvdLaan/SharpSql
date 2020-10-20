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
            if (sqlBuilder?.TableNameResolvePaths.Count > 0)
            {
                BuildMultiLayeredEntity(entity, reader, sqlBuilder);
            }
            else
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    SetEntityProperty(entity, reader, i);
                }
            }

            entity.IsNew = entity.PrimaryKey.Keys.Any(x => (int)entity[x.ColumnName] <= 0);

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

        private static void BuildMultiLayeredEntity<EntityType>(EntityType entity, IDataReader reader, SQLBuilder sqlBuilder)
            where EntityType : ORMEntity
        {
            var tableIndex = 0;

            foreach (var (name, _) in sqlBuilder.TableOrder)
            {
                var tableColumnCount = sqlBuilder.TableNameColumnCount[name];

                if (!sqlBuilder.TableNameColumnCount.ContainsKey(name)
                  || tableColumnCount == 0)
                {
                    continue;
                }

                var objectPath = sqlBuilder.TableNameResolvePaths.ContainsKey(name) ? sqlBuilder.TableNameResolvePaths[name] : string.Empty;
                if (!objectPath.StartsWith(SQLBuilder.MANY_TO_MANY_JOIN, StringComparison.Ordinal))
                {
                    var objectToFill = GetObjectAtPath(entity, objectPath);

                    for (int i = 0; i < tableColumnCount; i++)
                    {
                        SetEntityProperty(objectToFill, reader, i, tableIndex);
                    }
                }

                tableIndex += tableColumnCount;
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
                        SetEntityProperty(instance, _reader, index);
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
        internal static void SetEntityProperty(ORMEntity entity, IDataReader reader, int iteration, int tableIndex = 0)
        {
            var propertyName = reader.GetName(iteration + tableIndex);
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
                    var subEntity = Activator.CreateInstance(type.UnderlyingSystemType);

                    var fetchEntityByPrimaryKey = subEntity.GetType().BaseType
                        .GetMethod(nameof(ORMEntity.FetchEntityByPrimaryKey),
                        entity.PublicFlags,
                        Type.DefaultBinder,
                        new Type[] { typeof(object) },
                        null);

                    if (!ORMUtilities.IsUnitTesting)
                    {
                        if (reader.GetValue(iteration + tableIndex) == DBNull.Value)
                        {
                            break;
                        }

                        if (entity.DisableChangeTracking)
                        {
                            (subEntity as ORMEntity).DisableChangeTracking = entity.DisableChangeTracking;
                        }

                        value = fetchEntityByPrimaryKey.Invoke(subEntity, new object[] { reader.GetValue(iteration + tableIndex) });
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(reader.GetValue(iteration + tableIndex).ToString()))
                        {
                            break;
                        }
                        else
                        {
                            if (((ORMEntity)subEntity).PrimaryKey.Keys.Count == 1)
                            {
                                var subEntityIdType = subEntity.GetType().GetProperty(((ORMEntity)subEntity).PrimaryKey.Keys[0].ColumnName).PropertyType;
                                var id = Convert.ChangeType(reader.GetValue(iteration + tableIndex), subEntityIdType);

                                if (entity.DisableChangeTracking)
                                {
                                    (subEntity as ORMEntity).DisableChangeTracking = entity.DisableChangeTracking;
                                }

                                value = fetchEntityByPrimaryKey.Invoke(subEntity, new object[] { id });
                            }
                            else
                            {
                                // Combined primary key.
                                throw new NotImplementedException();
                            }
                        }
                    }

                    entity.EntityRelations.Add(value as ORMEntity);
                    break;
                default:
                    value = reader.GetValue(iteration + tableIndex);
                    break;
            }

            if (ORMUtilities.IsUnitTesting)
            {
                // Unit tests columns are all of type string, therefore they require to be converted to their respective type.
                if (Nullable.GetUnderlyingType(entityPropertyInfo.PropertyType) != null)
                {
                    value = Convert.ChangeType(value, Nullable.GetUnderlyingType(entityPropertyInfo.PropertyType));
                }
                else
                {
                    value = Convert.ChangeType(value, entityPropertyInfo.PropertyType);
                }
            }

            if (reader.GetValue(iteration + tableIndex) == DBNull.Value)
            {
                entityPropertyInfo.SetValue(entity, null);
            }
            else
            {
                entityPropertyInfo.SetValue(entity, value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ORMEntity GetObjectAtPath(ORMEntity entity, string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                foreach (var step in path.Split('.'))
                {
                    var property = entity.GetType().GetProperty(step, entity.PublicIgnoreCaseFlags);

                    var value = property.GetValue(entity);
                    if (value == null)
                    {
                        value = Activator.CreateInstance(property.PropertyType);
                        property.SetValue(entity, value);
                    }

                    entity = (ORMEntity)value;
                }
            }

            return entity;
        }
    }
}
