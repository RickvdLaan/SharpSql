using SharpSql.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace SharpSql;

/// <summary>
/// <para>
/// An internal memory database which represents a set of <see cref="SharpSqlEntity"/> objects for the unit tests.
/// </para>
/// <para>
/// The <see cref="MemoryEntityDatabase"/> simulates the database through pre-defined xml table records.
/// The <see cref="QueryBuilder"/> generates the query which can simply be tested via a simple assert:
/// "ExpectedQuery equals queryBuilder.GeneratedQuery.".
/// </para>
/// </summary>
internal class MemoryEntityDatabase : MemoryDatabase
{
    public MemoryEntityDatabase(Assembly externalAssembly) : base(externalAssembly) { }

    public static IDataReader FetchEntityById(string tableName, ISharpSqlPrimaryKey primaryKey, object id)
    {
        if (string.IsNullOrEmpty(tableName))
            throw new ArgumentNullException(nameof(tableName));
        if (primaryKey == null)
            throw new ArgumentNullException(nameof(primaryKey));
        if (id == null)
            throw new ArgumentNullException(nameof(id));

        var path = BasePath + tableName.ToUpperInvariant();
        var tableRecords = SharpSqlUtilities.MemoryEntityDatabase.MemoryTables.DocumentElement.SelectNodes(path);

        foreach (XmlElement record in tableRecords)
        {
            var xmlAttribute = record.GetAttributeNode(primaryKey.ColumnName);
            if (xmlAttribute != null && xmlAttribute.Value.Equals(id.ToString(), StringComparison.InvariantCultureIgnoreCase))
            {
                return ParseDataTableFromXmlRecord(record);
            }
        }

        return null;
    }

    public static IDataReader FetchEntityByCombinedId(string tableName, SharpSqlPrimaryKey primaryKey, List<object> ids)
    {
        if (string.IsNullOrEmpty(tableName))
            throw new ArgumentNullException(nameof(tableName));
        if (primaryKey == null || primaryKey.Keys.Count <= 1)
            throw new ArgumentNullException(nameof(primaryKey));
        if (ids == null)
            throw new ArgumentNullException(nameof(ids));
        if (ids.Count != primaryKey.Keys.Count)
            throw new ArgumentException($"{nameof(ids.Count)} does not equal {nameof(primaryKey.Keys.Count)}");

        var path = BasePath + tableName.ToUpperInvariant();
        var tableRecords = SharpSqlUtilities.MemoryEntityDatabase.MemoryTables.DocumentElement.SelectNodes(path);

        foreach (XmlElement record in tableRecords)
        {
            var attributes = new List<XmlAttribute>(primaryKey.Keys.Count);

            foreach (var key in primaryKey.Keys)
            {
                attributes.Add(record.GetAttributeNode(key.ColumnName));
            }

            for (int i = 0; i < attributes.Count; i++)
            {
                if (attributes[i] != null && attributes[i].Value.Equals(ids[i].ToString(), StringComparison.InvariantCultureIgnoreCase))
                {
                    if (i == attributes.Count - 1)
                    {
                        return ParseDataTableFromXmlRecord(record);
                    }

                    continue;
                }

                break;
            }
        }

        return null;
    }

    internal static List<string> FetchTableColumns(string tableName)
    {
        if (string.IsNullOrEmpty(tableName))
            throw new ArgumentNullException(nameof(tableName));

        var path = BasePath + tableName.ToUpperInvariant();
        var tableRecords = SharpSqlUtilities.MemoryEntityDatabase.MemoryTables.DocumentElement.SelectNodes(path);

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

    private static IDataReader ParseDataTableFromXmlRecord(XmlElement record)
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
                DataColumn missingColumn = new(columns[i]);
                dataSet.Tables[0].Columns.Add(missingColumn);
                missingColumn.SetOrdinal(i);
            }
        }

        return dataSet.Tables[0].CreateDataReader();
    }
}