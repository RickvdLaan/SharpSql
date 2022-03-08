using SharpSql.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace SharpSql;

public class SharpSqlPrimaryKey : IEqualityComparer<SharpSqlPrimaryKey>
{
    public int HashCode { get; private set; }

    public int Count => Keys.Count;

    public bool IsCombinedPrimaryKey { get { return Keys.Count > 1; } }

    public List<ISharpSqlPrimaryKey> Keys { get; private set; }

    public bool IsEmpty => Keys.Any(x => x.Value == null);

    public SharpSqlPrimaryKey(int totalAmountOfKeys)
    {
        Keys = new List<ISharpSqlPrimaryKey>(totalAmountOfKeys);
    }

    public override int GetHashCode()
    {
        return GetHashCode(this);
    }

    public int GetHashCode([DisallowNull] SharpSqlPrimaryKey obj) => obj.HashCode;

    public static bool operator ==(SharpSqlPrimaryKey x, SharpSqlPrimaryKey y)
    {
        return x.Equals(y);
    }

    public static bool operator !=(SharpSqlPrimaryKey x, SharpSqlPrimaryKey y)
    {
        return !x.Equals(y);
    }

    public bool Equals(SharpSqlPrimaryKey x, SharpSqlPrimaryKey y)
    {
        return x.HashCode == y.HashCode && Enumerable.SequenceEqual(x.Keys, y.Keys);
    }

    public override bool Equals(object obj)
    {
        //Check for null and compare run-time types.
        if ((obj == null) || obj is not SharpSqlPrimaryKey key)
        {
            return false;
        }
        else
        {
            return Equals(this, key);
        }
    }

    public void Add(string propertyName, string columnName, object value, bool isAutoIncrement)
    {
        Keys.Add(new PrimaryKey(propertyName, columnName, value, isAutoIncrement));
    }

    internal void Update(SharpSqlEntity entity)
    {
        var hashCode = new HashCode();

        foreach (ISharpSqlPrimaryKey key in Keys)
        {
            key.Value = entity[key.ColumnName];
            hashCode.Add(key.Value);
        }

        HashCode = hashCode.ToHashCode();
    }
}