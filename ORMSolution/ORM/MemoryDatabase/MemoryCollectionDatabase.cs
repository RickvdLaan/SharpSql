using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Xml;

namespace ORM
{
    /// <summary>
    /// An internal memory database for the ORMCollectionTests.
    /// </summary>
    internal class MemoryCollectionDatabase
    {
        private const string MemoryDatabase = "DATABASE";

        private const string RootMemoryTable = "DATA";

        private const string BasePath = "//" + MemoryDatabase + "/" + RootMemoryTable + "/";

        public XmlDocument MemoryTables { get; set; } = new XmlDocument();

        public MemoryCollectionDatabase()
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

        public DataTable Fetch(string memoryTableName)
        {
            var path = BasePath + memoryTableName.ToUpperInvariant();
            var tableRecords = ORMUtilities.MemoryCollectionDatabase.MemoryTables.DocumentElement.SelectNodes(path);

            var dataSet = new DataSet();

            foreach (XmlElement record in tableRecords)
            {
                var reader = new StringReader(record.OuterXml);
                var tempDataSet = new DataSet();
                tempDataSet.ReadXml(reader);
                dataSet.Merge(tempDataSet);
            }

            return dataSet.Tables[0];
        }
    }
}