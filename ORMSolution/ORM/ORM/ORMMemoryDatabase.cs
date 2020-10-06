using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Xml;

namespace ORM
{
    internal class ORMMemoryDatabase
    {
        public XmlDocument MemoryTables { get; set; } = new XmlDocument();

        private string MemoryDatabase => "DATABASE";

        private string RootMemoryTable => "DATA";

        private string BasePath => $"//{ MemoryDatabase }/{ RootMemoryTable }/";

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

        public DataTable FetchEntityById(string tableName, ORMPrimaryKey primaryKey, object id)
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
                    if (xmlAttribute != null && xmlAttribute.Value.Equals(id.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        var reader = new StringReader(record.OuterXml);
                        var dataSet = new DataSet();
                        dataSet.ReadXml(reader);

                        return dataSet.Tables[0];
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            return null;
        }

        public DataTable Fetch(SQLBuilder sqlBuilder)
        {
            var query = sqlBuilder.GeneratedQuery;

            var top = GetTopCount(query);

            var path = BasePath + sqlBuilder.TableAttribute.TableName.ToUpperInvariant();

            // Cloning the XmlDocument so it doesn't affect the main MemoryDatabase.
            var clonedXmlDocument = ORMUtilities.MemoryDatabase.MemoryTables.Clone();

            if (sqlBuilder.Joins.Any())
            {
                foreach (var join in sqlBuilder.Joins)
                {
                    path += "|" +BasePath + join.RightTableAttribute.TableName.ToUpperInvariant();
                }
            }
            
            var tableRecords = clonedXmlDocument.SelectNodes(path);

            var dataSet = new DataSet();

            var enumerator = tableRecords.GetEnumerator();
            enumerator.MoveNext();
            for (int i = 0; i < tableRecords.Count && (top == -1 || i < top); i++)
            {
                var record = RemoveUnnecessaryColumns(enumerator.Current as XmlElement, query);

                var stringReader = new StringReader(record.OuterXml);

                var tempDataSet = new DataSet();
                tempDataSet.ReadXml(stringReader);
                var rows = new DataRow[tempDataSet.Tables[0].Rows.Count];
                tempDataSet.Tables[0].Rows.CopyTo(rows, 0);
                dataSet.Merge(rows);

                enumerator.MoveNext();
            }

            if (sqlBuilder.Joins.Any())
            {
                int i = 1;

                foreach (var join in sqlBuilder.Joins)
                {
                    var relationName = $"{join.LeftTableAttribute.TableName}_Relation_{join.RightTableAttribute.TableName}";
                    var left = dataSet.Tables[join.LeftTableAttribute.TableName].Columns["Organisation"];
                    var right = dataSet.Tables[join.RightTableAttribute.TableName].Columns["Id"];
                    dataSet.Relations.Add(relationName, left, right);
                    dataSet.AcceptChanges();
                    i++;
                }
            }

            return dataSet.Tables[0];
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