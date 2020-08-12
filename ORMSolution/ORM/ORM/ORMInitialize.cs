using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System;
using ORM.Attributes;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ORMNUnit")]

namespace ORM
{
    public sealed class ORMInitialize
    {
        internal ORMInitialize(params string[] xmlFilePaths)
        {
            ORMUtilities.MemoryDatabase = new ORMMemoryDatabase();
            ORMUtilities.MemoryDatabase.LoadMemoryTables(xmlFilePaths);

            new ORMInitialize();
        }

        public ORMInitialize(IConfiguration configuration = null)
        {
            new ORMUtilities(configuration);

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    var attributes = type.GetCustomAttributes(typeof(ORMTableAttribute), true);
                    if (attributes.Length > 0)
                    {
                        var tableAttribute = (attributes.First() as ORMTableAttribute);

                        if (tableAttribute.CollectionTypeLeft  == null
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
                                var columns = ORMUtilities.MemoryDatabase.FetchTableColumns(tableAttribute.TableName);

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
}
