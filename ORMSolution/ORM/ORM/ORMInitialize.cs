﻿using Microsoft.Extensions.Configuration;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System;
using ORM.Attributes;
using System.Linq;

[assembly: InternalsVisibleTo("ORMNUnit")]

namespace ORM
{
    public sealed class ORMInitialize
    {
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

                        if (tableAttribute.CollectionTypeLeft == null
                         && tableAttribute.CollectionTypeRight == null)
                        {
                            ORMUtilities.CollectionEntityRelations.Add(tableAttribute.CollectionType, tableAttribute.EntityType);
                            ORMUtilities.CollectionEntityRelations.Add(tableAttribute.EntityType, tableAttribute.CollectionType);

                            if (!ORMUtilities.CachedColumns.ContainsKey(tableAttribute.CollectionType)
                             && !ORMUtilities.CachedColumns.ContainsKey(tableAttribute.EntityType))
                            {
                                using (var connection = new SQLConnection())
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
                                        columns.Add(rows[i][0].ToString());
                                    }

                                    ORMUtilities.CachedColumns.Add(tableAttribute.CollectionType, columns);
                                    ORMUtilities.CachedColumns.Add(tableAttribute.EntityType, columns);
                                }
                            }
                        }
                        else
                        {
                            ORMUtilities.ManyToManyRelations.Add(tableAttribute.CollectionType, (tableAttribute.CollectionTypeLeft, tableAttribute.CollectionTypeRight));
                        }
                    }
                }
            }
        }
    }
}
