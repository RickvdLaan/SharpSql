using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;

namespace SharpSql
{
    internal class MemoryDatabase
    {
        public Assembly ExternalAssembly = null;

        protected const string RootMemoryDatabase = "DATABASE";

        protected const string RootMemoryTable = "DATA";

        protected const string BasePath = "//" + RootMemoryDatabase + "/" + RootMemoryTable + "/";

        public XmlDocument MemoryTables { get; set; } = new XmlDocument();

        public MemoryDatabase(Assembly externalAssembly)
        {
            ExternalAssembly = externalAssembly;

            MemoryTables.AppendChild(MemoryTables.CreateElement(RootMemoryDatabase));
        }

        public void LoadMemoryTables(List<string> xmlFilePaths)
        {
            foreach (var xmlFilePath in xmlFilePaths)
            {
                ImportXml(xmlFilePath);
            }
        }

        protected void ImportXml(string xmlFilePath)
        {
            var xmlDocument = new XmlDocument();

            using (var streamReader = new StreamReader(ExternalAssembly.GetManifestResourceStream(xmlFilePath)))
            {
                xmlDocument.LoadXml(streamReader.ReadToEnd());
            }

            ImportMemoryTable(xmlDocument);
        }

        protected void ImportMemoryTable(XmlDocument xmlDocument)
        {
            var tableRows = xmlDocument.SelectSingleNode($"//{ RootMemoryTable }");

            foreach (XmlNode tableRow in tableRows.ChildNodes)
            {
                XmlNode node = MemoryTables.CreateNode(XmlNodeType.Element, RootMemoryTable, null);
                node.AppendChild(MemoryTables.ImportNode(tableRow, true));
                MemoryTables.DocumentElement.AppendChild(node);
            }
        }
    }
}
