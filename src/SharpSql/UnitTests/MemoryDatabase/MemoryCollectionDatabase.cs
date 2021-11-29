﻿using System.Data;
using System.IO;
using System.Reflection;
using System.Xml;

namespace SharpSql
{
    /// <summary>
    /// <para>
    /// An internal memory database which represents an <see cref="SharpSqlCollection{EntityType}"/> dataset for the unit tests.
    /// </para>
    /// /// <para>
    /// The <see cref="MemoryCollectionDatabase"/> simulates the datatable through pre-defined xml datasets.
    /// This way mapping the POCO's can be unit tested, since we know what dataset should be returned based on a query.
    /// The <see cref="QueryBuilder"/> generates the query which can simply be tested via a simple assert: 
    /// "ExpectedQuery equals queryBuilder.GeneratedQuery.".
    /// </para>
    /// </summary>
    internal class MemoryCollectionDatabase : MemoryDatabase
    {
        public MemoryCollectionDatabase(Assembly externalAssembly) : base(externalAssembly) { }

        public static DataTable Fetch(string memoryTableName)
        {
            var path = BasePath + memoryTableName.ToUpperInvariant();
            var tableRecords = SharpSqlUtilities.MemoryCollectionDatabase.MemoryTables.DocumentElement.SelectNodes(path);

            if (tableRecords.Count == 0)
                throw new System.Exception($"Could not find any records (nodes) for { memoryTableName } in { memoryTableName }.xml.");

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