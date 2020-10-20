using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Xml;

namespace ORM
{
    /// <summary>
    /// Internally used to fetch data from the xml memory tables for the unit tests.
    /// </summary>
    internal class ORMMemoryDatabase
    {
        private const string MemoryDatabase = "DATABASE";

        private const string RootMemoryTable = "DATA";

        private const string BasePath = "//" + MemoryDatabase + "/" + RootMemoryTable + "/";

        public XmlDocument MemoryTables { get; set; } = new XmlDocument();

        public ORMMemoryDatabase()
        {
            MemoryTables.AppendChild(MemoryTables.CreateElement(MemoryDatabase));
        }

        public void LoadMemoryTables(params string[] xmlFilePaths)
        {
            foreach (var xmlFilePath in xmlFilePaths)
            {
                ImportXml(xmlFilePath);
            }
        }

        private void ImportXml(string xmlFilePath)
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.Load(xmlFilePath);
            ImportMemoryTable(xmlDocument);
        }

        private void ImportMemoryTable(XmlDocument xmlDocument)
        {
            var tableRows = xmlDocument.SelectSingleNode($"//{ RootMemoryTable }");

            foreach (XmlNode tableRow in tableRows.ChildNodes)
            {
                XmlNode node = MemoryTables.CreateNode(XmlNodeType.Element, RootMemoryTable, null);
                node.AppendChild(MemoryTables.ImportNode(tableRow, true));
                MemoryTables.DocumentElement.AppendChild(node);
            }
        }

        public IDataReader FetchEntityById(string tableName, ORMPrimaryKey primaryKey, object id)
        {
            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentNullException();
            if (primaryKey == null)
                throw new ArgumentNullException();
            if (id == null)
                throw new ArgumentNullException();

            var path = BasePath + tableName.ToUpperInvariant();
            var tableRecords = ORMUtilities.MemoryDatabase.MemoryTables.DocumentElement.SelectNodes(path);

            foreach (XmlElement record in tableRecords)
            {
                if (primaryKey.Keys.Count == 1)
                {
                    var xmlAttribute = record.GetAttributeNode(primaryKey.Keys[0].ColumnName);
                    if (xmlAttribute != null && xmlAttribute.Value.Equals(id.ToString(), StringComparison.InvariantCultureIgnoreCase))
                    {
                        var reader = new StringReader(record.OuterXml);
                        var dataSet = new DataSet();
                        dataSet.ReadXml(reader);

                        return dataSet.Tables[0].CreateDataReader();
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            return null;
        }

        public IDataReader Fetch<EntityType>(SQLBuilder sqlBuilder)
            where EntityType : ORMEntity
        {
            var query = sqlBuilder.GeneratedQuery;

            var top = GetTopCount(query);

            var path = BasePath + sqlBuilder.TableAttribute.TableName.ToUpperInvariant();

            // Cloning the XmlDocument so it doesn't affect the main MemoryDatabase.
            var clonedXmlDocument = ORMUtilities.MemoryDatabase.MemoryTables.Clone();

            List<ORMEntity> objectTypes = new List<ORMEntity>
            {
                (ORMEntity)Activator.CreateInstance(sqlBuilder.TableAttribute.EntityType)
            };

            // If the sqlBuilder contains any joins, append them.
            if (sqlBuilder.Joins.Any())
            {
                foreach (var join in sqlBuilder.Joins)
                {
                    path += "|" + BasePath + join.RightTableAttribute.TableName.ToUpperInvariant();

                    objectTypes.Add((ORMEntity)Activator.CreateInstance(join.RightTableAttribute.EntityType));
                }
            }

            var table = new DataTable();

            // All of the table records.
            var tableRecords = clonedXmlDocument.SelectNodes(path);

            var enumerator = tableRecords.GetEnumerator();
            enumerator.MoveNext();
            for (int i = 0; i < tableRecords.Count && (top == -1 || i < top); i++)
            {
                if (sqlBuilder.Joins.Any() && sqlBuilder.Joins.Any(x => tableRecords[i].Name.Equals(x.RightTableAttribute.TableName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    continue;
                }

                var record = RemoveUnnecessaryColumns(enumerator.Current as XmlElement, query);

                var stringReader = new StringReader(record.OuterXml);

                var tempDataSet = new DataSet();
                tempDataSet.ReadXml(stringReader);
                var rows = new DataRow[tempDataSet.Tables[0].Rows.Count];
                tempDataSet.Tables[0].Rows.CopyTo(rows, 0);

                var tableName = rows[0].Table.TableName;
                var entity = objectTypes.First(x => ORMUtilities.CollectionEntityRelations[x.GetType()].Name.Equals(tableName, StringComparison.InvariantCultureIgnoreCase)).ShallowCopy();
                var primaryKey = entity.PrimaryKey;
                var columns = FetchTableColumns(tableName);
                var index = columns.FindIndex(x => x == primaryKey.Keys[0].ColumnName);
                var id = rows[0].ItemArray[index];

                // Combined primary key not yet implemented.
                if (primaryKey.Count > 1)
                    throw new NotImplementedException();

                var reader = FetchEntityById(tableName, primaryKey, id);
                reader.Read();
                var row = table.NewRow();

                var itemArray = rows[0].ItemArray.ToList();

                if (i == 0)
                {
                    for (int j = 0; j < reader.FieldCount; j++)
                    {
                        table.Columns.Add(tableName + '|' + columns[j]);

                        if (sqlBuilder.Joins.Any() && j == 0)
                        {
                            foreach (var join in sqlBuilder.Joins)
                            {
                                var subentity = objectTypes.First(x => ORMUtilities.CollectionEntityRelations[x.GetType()].Name.Equals(tableName, StringComparison.InvariantCultureIgnoreCase)).ShallowCopy();
                                var subprimaryKey = subentity.PrimaryKey;
                                var subcolumns = FetchTableColumns(join.RightTableAttribute.TableName);
                                var subindex = columns.FindIndex(x => x == subprimaryKey.Keys[0].ColumnName);
                                var subid = entity[join.LeftPropertyInfo.Name];

                                var retrievedid = reader.GetValue(index);

                                var reader2 = FetchEntityById(join.RightTableAttribute.TableName, subprimaryKey, retrievedid);
                                reader2.Read();
                                for (int k = 0; k < reader2.FieldCount; k++)
                                {
                                    itemArray.Add(reader2.GetValue(k));
                                }

                            }
                        }
                    }
                    foreach (var join in sqlBuilder.Joins)
                    {
                        if (i == 0)
                        {
                            foreach (var columnName in FetchTableColumns(join.RightTableAttribute.TableName))
                            {
                                table.Columns.Add(join.RightTableAttribute.TableName + '|' + columnName);
                            }
                        }
                    }
                }

                row.ItemArray = itemArray.ToArray();

                table.Rows.Add(row);

                enumerator.MoveNext();
            }


            return table.CreateDataReader();
        }

        private IDataReader CreateDataTable<EntityType>(ORMCollection<EntityType> values)
             where EntityType : ORMEntity
        {
            throw new NotImplementedException();
        }

        private int GetTopCount(string query)
        {
            if (query.Contains("SELECT TOP"))
            {
                var startIndex = query.IndexOf('(') + 1;
                var length = query.IndexOf(')') - query.IndexOf('(') - 1;

                return Convert.ToInt32(query.Substring(startIndex, length));
            }

            return -1;
        }

        private XmlElement RemoveUnnecessaryColumns(XmlElement record, string query)
        {
            if (!query.Contains("SELECT *"))
            {
                var startIndex = query.IndexOf("SELECT") + 6;
                var length = query.IndexOf("FROM") - startIndex;

                if (query.StartsWith("SELECT TOP")
                 && query.Substring(startIndex, length).Contains("*"))
                {
                    return record;
                }

                var columns = query.Substring(startIndex, length).Trim().Split(',');

                for (int i = 0; i < columns.Length; i++)
                {
                    var columnStartIndex = columns[i].LastIndexOf('[') + 1;
                    var columnlength = columns[i].LastIndexOf(']') - columnStartIndex;

                    columns[i] = columns[i].Substring(columnStartIndex, columnlength);
                }

                var attributesToRemove = new List<XmlAttribute>();
                foreach (XmlAttribute attribute in record.Attributes)
                {
                    if (!columns.Any(x => x.Equals(attribute.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        attributesToRemove.Add(attribute);
                    }
                }

                foreach (var attribute in attributesToRemove)
                {
                    record.Attributes.Remove(attribute);
                }
            }

            return record;
        }

        public List<string> FetchTableColumns(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentNullException();

            var path = BasePath + tableName.ToUpperInvariant();
            var tableRecords = ORMUtilities.MemoryDatabase.MemoryTables.DocumentElement.SelectNodes(path);

            if (tableRecords.Count == 0)
            {
                // Return null when no xml records were found for the current table.
                return null;
            }

            var columns = new List<string>(tableRecords[0].Attributes.Count);

            if (tableRecords[0].Name.Equals(tableName, StringComparison.OrdinalIgnoreCase))
            {
                foreach (XmlAttribute column in tableRecords[0].Attributes)
                {
                    columns.Add(column.Name);
                }
            }

            return columns;
        }
    }
}