using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
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
                        var theReader = new StringReader(record.OuterXml);
                        var theDataSet = new DataSet();
                        theDataSet.ReadXml(theReader);

                        return theDataSet.Tables[0];
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
            var tableRecords = ORMUtilities.MemoryDatabase.MemoryTables.DocumentElement.SelectNodes(path);

            if (tableRecords.Count == 0)
            {
                // Return null when no xml record was found for the current table.
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
