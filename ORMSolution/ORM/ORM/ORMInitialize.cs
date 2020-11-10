using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System;
using ORM.Attributes;
using System.Linq;
using System.Runtime.CompilerServices;
using System.IO;
using System.Reflection;

[assembly: InternalsVisibleTo("ORMNUnit")]

namespace ORM
{
    public sealed class ORMInitialize
    {
        internal ORMInitialize(List<string> xmlEntityFilePaths, List<string> xmlCollectionFilePaths)
        {
            ORMUtilities.MemoryEntityDatabase = new MemoryEntityDatabase();
            ORMUtilities.MemoryEntityDatabase.LoadMemoryTables(xmlEntityFilePaths);

            ORMUtilities.MemoryCollectionDatabase = new MemoryCollectionDatabase();
            ORMUtilities.MemoryCollectionDatabase.LoadMemoryTables(xmlCollectionFilePaths);

            new ORMInitialize(configuration: null, loadAllReferencedAssemblies: true);
        }
        
        private void LoadAllReferencedAssemblies()
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
                    // Because of this bug we can't only load what we know we actually need.
                    var assemblyBytes = File.ReadAllBytes(referencedPath);

                    // .NET Core only: This member is not supported.
                    var assembly = Assembly.ReflectionOnlyLoad(assemblyBytes);

                    if (assembly.GetReferencedAssemblies().Contains(Assembly.GetAssembly(typeof(ORMEntity)).GetName()))
                    {
                        AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(referencedPath));
                    }
                }
                else
                {
                    // Currently the only way, untill we find another way to do this through meta-data.
                    AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(referencedPath));
                }
            }
        }

        public ORMInitialize(IConfiguration configuration = null, bool loadAllReferencedAssemblies = false)
        {
            new ORMUtilities(configuration);

            if (loadAllReferencedAssemblies)
            {
                LoadAllReferencedAssemblies();
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes().Where(type => type.GetCustomAttributes(typeof(ORMTableAttribute), true).Length > 0))
                {
                    var tableAttribute = type.GetCustomAttribute(typeof(ORMTableAttribute), true) as ORMTableAttribute;

                    if (tableAttribute.CollectionTypeLeft == null
                     && tableAttribute.CollectionTypeRight == null)
                    {
                        ORMUtilities.CollectionEntityRelations.Add(tableAttribute.CollectionType, tableAttribute.EntityType);
                        ORMUtilities.CollectionEntityRelations.Add(tableAttribute.EntityType, tableAttribute.CollectionType);
                    }
                    else
                    {
                        ORMUtilities.CollectionEntityRelations.Add(tableAttribute.CollectionType, tableAttribute.EntityType);
                        ORMUtilities.CollectionEntityRelations.Add(tableAttribute.EntityType, tableAttribute.CollectionType);
                        ORMUtilities.ManyToManyRelations.Add((tableAttribute.CollectionTypeLeft, tableAttribute.CollectionTypeRight), tableAttribute);
                        ORMUtilities.ManyToManyRelations.Add((tableAttribute.CollectionTypeRight, tableAttribute.CollectionTypeLeft), tableAttribute);
                    }
                    if (!ORMUtilities.CachedColumns.ContainsKey(tableAttribute.CollectionType)
                     && !ORMUtilities.CachedColumns.ContainsKey(tableAttribute.EntityType))
                    {
                        if (!ORMUtilities.IsUnitTesting)
                        {
                            var sqlBuilder = new SQLBuilder();
                            sqlBuilder.BuildQuery(tableAttribute, null, null, null, null, 0);
                            var rows = ORMUtilities.ExecuteDirectQuery(sqlBuilder.GeneratedQuery)
                                  .CreateDataReader()
                                  .GetSchemaTable()
                                  .Rows;

                            var columns = new List<string>(rows.Count);

                            for (int i = 0; i < rows.Count; i++)
                            {
                                columns.Add((string)rows[i][0]);
                            }

                            ORMUtilities.CachedColumns.Add(tableAttribute.CollectionType, columns);
                            ORMUtilities.CachedColumns.Add(tableAttribute.EntityType, columns);
                        }
                        else
                        {
                            var columns = ORMUtilities.MemoryEntityDatabase.FetchTableColumns(tableAttribute.TableName);

                            if (columns != null)
                            {
                                ORMUtilities.CachedColumns.Add(tableAttribute.CollectionType, columns);
                                ORMUtilities.CachedColumns.Add(tableAttribute.EntityType, columns);
                            }
                        }
                    }
                }
            }
        }
    }
}
