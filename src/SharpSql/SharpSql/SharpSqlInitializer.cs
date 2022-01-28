using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System;
using SharpSql.Attributes;
using System.Linq;
using System.IO;
using System.Reflection;

namespace SharpSql;

public sealed class SharpSqlInitializer
{
    internal SharpSqlInitializer(Assembly callingAssembly, string xmlEntityFilePath, string xmlCollectionFilePath)
    {
        SharpSqlUtilities.MemoryEntityDatabase = new MemoryEntityDatabase(Assembly.GetCallingAssembly());
        SharpSqlUtilities.MemoryEntityDatabase.LoadMemoryTables(LoadMemoryDatabase(callingAssembly, xmlEntityFilePath));

        SharpSqlUtilities.MemoryCollectionDatabase = new MemoryCollectionDatabase(Assembly.GetCallingAssembly());
        SharpSqlUtilities.MemoryCollectionDatabase.LoadMemoryTables(LoadMemoryDatabase(callingAssembly, xmlCollectionFilePath));

        _ = new SharpSqlInitializer(configuration: null, loadAllReferencedAssemblies: true, allowAnonymousTypes: true);
    }

    private static List<string> LoadMemoryDatabase(Assembly callingAssembly, string folder)
    {
        var files = new List<string>();

        foreach (string resource in callingAssembly.GetManifestResourceNames().Where(resource => resource.Contains(folder)))
        {
            files.Add(resource);
        }

        return files;
    }

    private static void LoadAllReferencedAssemblies()
    {
        var referencedPaths = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll");

        foreach (var referencedPath in referencedPaths)
        {
            var DotNetCoreBugFixed = false;
            if (DotNetCoreBugFixed)
            {
                // .Net Core does not support the ReflectionOnlyLoad feature of NETFX. ReflectionOnlyLoad was a
                // feature for inspecting managed assemblies using the familiar Reflection api (Type, MethodInfo, etc.)

                // The TypeLoader class is the .NET Core replacement for this feature.

                // MetadataLoadContext doesn't work because this isn't a NuGet package.

                // Links:
                // https://github.com/dotnet/corefxlab/blob/master/docs/specs/typeloader.md
                // https://github.com/dotnet/runtime/issues/15033
                // https://github.com/dotnet/runtime/issues/31200
                // Because of this 'bug' we can't only load what we know we actually need.
                // -Rick, 25 September 2020
                var assemblyBytes = File.ReadAllBytes(referencedPath);

                // .NET Core only: This member is not supported.
#pragma warning disable SYSLIB0018 // Type or member is obsolete
                var assembly = Assembly.ReflectionOnlyLoad(assemblyBytes);
#pragma warning restore SYSLIB0018 // Type or member is obsolete

                if (assembly.GetReferencedAssemblies().Contains(Assembly.GetAssembly(typeof(SharpSqlEntity)).GetName()))
                {
                    AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(referencedPath));
                }
            }
            else
            {
                // Currently the only way, untill we find another way to do this through meta-data.
                // -Rick, 25 September 2020
                AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(referencedPath));
            }
        }
    }

    // @Todo: refactor to a smaller method, in which each cache type has its own method.
    // @Todo: create unit tests for each smaller method. (validate cache)
    public SharpSqlInitializer(IConfiguration configuration = null, bool loadAllReferencedAssemblies = false, bool allowAnonymousTypes = false)
    {
        _ = new DatabaseUtilities(configuration);
        _ = new SharpSqlUtilities();

        if (loadAllReferencedAssemblies)
        {
            LoadAllReferencedAssemblies();
        }

        SharpSqlUtilities.AllowAnonymouseTypes = allowAnonymousTypes;

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var type in assembly.GetTypes().Where(type => type.GetCustomAttributes(typeof(SharpSqlTableAttribute), true).Length > 0))
            {
                var tableAttribute = type.GetCustomAttribute(typeof(SharpSqlTableAttribute), true) as SharpSqlTableAttribute;

                var constructor = tableAttribute.EntityType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                if (constructor == null)
                {
                    throw new Exception($"Entity { tableAttribute.EntityType.Name } requires a (private) parameterless constructor.");
                }

                if (tableAttribute.CollectionTypeLeft == null
                 && tableAttribute.CollectionTypeRight == null)
                {
                    SharpSqlUtilities.CollectionEntityRelations.Add(tableAttribute.CollectionType, tableAttribute.EntityType);
                    SharpSqlUtilities.CollectionEntityRelations.Add(tableAttribute.EntityType, tableAttribute.CollectionType);
                }
                else
                {
                    SharpSqlUtilities.CollectionEntityRelations.Add(tableAttribute.CollectionType, tableAttribute.EntityType);
                    SharpSqlUtilities.CollectionEntityRelations.Add(tableAttribute.EntityType, tableAttribute.CollectionType);
                    SharpSqlUtilities.ManyToManyRelations.Add((tableAttribute.CollectionTypeLeft, tableAttribute.CollectionTypeRight), tableAttribute);
                    SharpSqlUtilities.ManyToManyRelations.Add((tableAttribute.CollectionTypeRight, tableAttribute.CollectionTypeLeft), tableAttribute);
                    SharpSqlUtilities.CachedManyToMany.Add(tableAttribute.CollectionType, default);
                    SharpSqlUtilities.CachedManyToMany.Add(SharpSqlUtilities.CollectionEntityRelations[tableAttribute.CollectionType], default);
                }
                if (!SharpSqlUtilities.CachedColumns.ContainsKey(tableAttribute.CollectionType)
                 && !SharpSqlUtilities.CachedColumns.ContainsKey(tableAttribute.EntityType))
                {
                    if (!UnitTestUtilities.IsUnitTesting)
                    {
                        var queryBuilder = new QueryBuilder();
                        queryBuilder.BuildQuery(tableAttribute, null, null, null, null, 0);
                        var rows = DatabaseUtilities.ExecuteDirectQuery(queryBuilder.GeneratedQuery)
                              .CreateDataReader()
                              .GetSchemaTable()
                              .Rows;

                        var uniqueConstraints = DatabaseUtilities.ExecuteDirectQuery(QueryBuilder.ColumnConstraintInformation(tableAttribute.TableName));

                        var columns = new List<string>(rows.Count);

                        for (int i = 0; i < rows.Count; i++)
                        {
                            for (int j = 0; j < uniqueConstraints.Rows.Count; j++)
                            {
                                if (uniqueConstraints.Rows[j][3].Equals(rows[i][0]))
                                {
                                    SharpSqlUtilities.UniqueConstraints.Add((tableAttribute.EntityType, (string)rows[i][0]));
                                    break;
                                }
                            }

                            columns.Add((string)rows[i][0]);
                        }

                        CreateColumnsCache(tableAttribute, columns);
                    }
                    else
                    {
                        CreateColumnsCache(tableAttribute, MemoryEntityDatabase.FetchTableColumns(tableAttribute.TableName));
                    }
                }
            }
        }

        CreatePrimaryKeyCache();
    }

    // @Todo: create unit test for CreateColumnsCache
    private static void CreateColumnsCache(SharpSqlTableAttribute tableAttribute, List<string> columnNames)
    {
        foreach (var columnName in columnNames)
        {
            foreach (var property in tableAttribute.EntityType.GetProperties())
            {
                var columnAttribute = property.GetCustomAttributes(typeof(SharpSqlColumnAttribute), false).FirstOrDefault() as SharpSqlColumnAttribute;

                var columnNameToUse = columnName;

                if (columnAttribute != null)
                {
                    columnNameToUse = columnAttribute.ColumnName;
                }

                if (property.Name == columnName || columnAttribute?.ColumnName == columnName)
                {
                    if (property.GetCustomAttributes(typeof(SharpSqlForeignKeyAttribute), false).FirstOrDefault() is SharpSqlForeignKeyAttribute)
                    {
                        SharpSqlUtilities.CachedColumns.AddColumnCache(tableAttribute.EntityType, columnNameToUse, ColumnType.Join);
                        break;
                    }
                    else if (property.GetCustomAttributes(typeof(SharpSqlManyToManyAttribute), false).FirstOrDefault() is SharpSqlManyToManyAttribute)
                    {
                        SharpSqlUtilities.CachedColumns.AddColumnCache(tableAttribute.EntityType, columnNameToUse, ColumnType.ManyToMany);
                        break;
                    }
                    else
                    {
                        SharpSqlUtilities.CachedColumns.AddColumnCache(tableAttribute.EntityType, columnNameToUse, ColumnType.Default);
                        break;
                    }
                }
            }
        }
    }

    // @Todo: create unit test CreatePrimaryKeyCache
    private static void CreatePrimaryKeyCache()
    {
        var entityTypes = SharpSqlUtilities.CollectionEntityRelations.Where(x => x.Key.IsAssignableTo(typeof(SharpSqlEntity)));

        foreach (var entityType in entityTypes)
        {
            SharpSqlUtilities.CachedPrimaryKeys.Add(entityType.Key, (Activator.CreateInstance(entityType.Key, true) as SharpSqlEntity).PrimaryKey);
        }
    }
}