﻿using SharpSql.Attributes;
using SharpSql.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace SharpSql;

public class SharpSqlPrimaryKey : IEqualityComparer<SharpSqlPrimaryKey>
{
    public int HashCode { get; private set; }

    public int Count => Keys.Count;

    public bool IsCombinedPrimaryKey { get { return Keys.Count > 1; } }

    public List<ISharpSqlPrimaryKey> Keys { get; private set; }

    public bool IsEmpty => Keys.Any(x => x.Value == null);

    internal SharpSqlPrimaryKey() { }

    public SharpSqlPrimaryKey(int totalAmountOfKeys)
    {
        Keys = new List<ISharpSqlPrimaryKey>(totalAmountOfKeys);
    }

    internal SharpSqlPrimaryKey(IDataReader reader, int[] primaryKeyIndexes)
        : this(primaryKeyIndexes.Length)
    {
        if (primaryKeyIndexes == null)
        {
            throw new ArgumentException("Primary keys not set");
        }

        var hashCode = new HashCode();
        for (int i = 0; i < primaryKeyIndexes.Length; i++)
        {
            Add(string.Empty, reader.GetName(i), reader.GetValue(i), false);
            hashCode.Add(reader.GetValue(i));
        }

        HashCode = hashCode.ToHashCode();
    }

    public int GetHashCode(SharpSqlPrimaryKey obj) => obj.HashCode;

    public bool Equals(SharpSqlPrimaryKey x, SharpSqlPrimaryKey y) => x.HashCode == y.HashCode && Enumerable.SequenceEqual(x.Keys, y.Keys);

    public void Add(string propertyName, string columnName, object value, bool isAutoIncrement)
    {
        Keys.Add(new PrimaryKey(propertyName, columnName, value, isAutoIncrement));
    }

    internal static int[] DeterminePrimaryKeyIndexes(IDataReader reader, SharpSqlEntity entity)
    {
        var pks = entity.GetPrimaryKeyPropertyInfo();
        var primaryKeyIndexes = new int[pks.Length];
        int found = 0;

        for (int i = 0; i < reader.FieldCount && found < pks.Length; i++)
        {
            var name = reader.GetName(i);

            var columnAttribute = pks[i].GetCustomAttributes(typeof(SharpSqlColumnAttribute), false).FirstOrDefault() as SharpSqlColumnAttribute;

            if ((columnAttribute != null && columnAttribute.ColumnName == name)
              || pks.Any(x => string.Compare(x.Name, name, true) == 0))
            {
                primaryKeyIndexes[found] = i;
                found++;
            }
        }

        if (found != pks.Length)
        {
            throw new ArgumentException($"Expected {pks.Length} primary key columns but recieved {found}");
        }

        return primaryKeyIndexes;
    }
}