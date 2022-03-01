using System;
using System.Collections.Generic;

namespace SharpSql;

internal static class DictionaryExtensions
{
    internal static void AddColumnCache(this Dictionary<Type, Dictionary<string, ColumnType>> dictionary, Type entityKey, string columnKey, ColumnType columnType)
    {
        if (dictionary.ContainsKey(entityKey))
        {
            dictionary[entityKey].Add(columnKey, columnType);
        }
        else
        {
            dictionary.Add(entityKey, new Dictionary<string, ColumnType>() { { columnKey, columnType } });
        }
    }
}