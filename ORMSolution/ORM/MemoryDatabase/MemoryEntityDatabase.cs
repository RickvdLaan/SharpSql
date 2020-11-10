using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Xml;

namespace ORM
{
    /// <summary>
    /// An internal memory database for the ORMEntityTests.
    /// </summary>
    internal class MemoryEntityDatabase
    {
        private const string MemoryDatabase = "DATABASE";

        private const string RootMemoryTable = "DATA";

        private const string BasePath = "//" + MemoryDatabase + "/" + RootMemoryTable + "/";

        public XmlDocument MemoryTables { get; set; } = new XmlDocument();

        public MemoryEntityDatabase()
        {
            MemoryTables.AppendChild(MemoryTables.CreateElement(MemoryDatabase));
        }

        public void LoadMemoryTables(List<string> xmlFilePaths)
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
            var tableRecords = ORMUtilities.MemoryEntityDatabase.MemoryTables.DocumentElement.SelectNodes(path);

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

                        var columns = FetchTableColumns(record.Name);

                        if (dataSet.Tables[0].Columns.Count < columns.Count)
                        {
                            for (int i = 0; i < columns.Count; i++)
                            {
                                if (string.Equals(dataSet.Tables[0].Columns[i].ColumnName, columns[i], StringComparison.InvariantCultureIgnoreCase))
                                {
                                    continue;
                                }

                                // When nullable field is null in the xml we need to insert at i.
                                DataColumn missingColumn = new DataColumn(columns[i]);
                                dataSet.Tables[0].Columns.Add(missingColumn);
                                missingColumn.SetOrdinal(i);
                            }
                        }

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

        public List<string> FetchTableColumns(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentNullException();

            var path = BasePath + tableName.ToUpperInvariant();
            var tableRecords = ORMUtilities.MemoryEntityDatabase.MemoryTables.DocumentElement.SelectNodes(path);

            if (tableRecords.Count == 0)
            {
                // Return null when no xml records were found for the current table.
                return null;
            }

            var maxColumns = tableRecords.Cast<XmlNode>().Max(x => x.Attributes.Count);
            var columns = new List<string>(maxColumns);

            for (int i = 0; i < tableRecords.Count; i++)
            {
                if (tableRecords[i].Attributes.Count == maxColumns)
                {
                    if (tableRecords[0].Name.Equals(tableName, StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (XmlAttribute column in tableRecords[0].Attributes)
                        {
                            columns.Add(column.Name);
                        }
                    }

                    break;
                }
            }

            return columns;
        }
    }
}