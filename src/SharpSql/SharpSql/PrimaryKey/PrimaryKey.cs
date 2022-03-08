using SharpSql.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SharpSql;

internal struct PrimaryKey : IEqualityComparer<PrimaryKey>, ISharpSqlPrimaryKey
{
    public string PropertyName { get; set; }

    public string ColumnName { get; set; }

    public object Value { get; set; }

    public bool IsAutoIncrement { get; set; }

    public PrimaryKey(string propertyName, string columnName, object id, bool isAutoIncrement)
    {
        PropertyName = propertyName;
        ColumnName = columnName;
        Value = id;
        IsAutoIncrement = isAutoIncrement;
    }

    public override string ToString()
    {
        return $"{ColumnName}: {Value} in {PropertyName}";
    }

    public static bool operator ==(PrimaryKey x, PrimaryKey y)
    {
        return x.Equals(y);
    }

    public static bool operator !=(PrimaryKey x, PrimaryKey y)
    {
        return !x.Equals(y);
    }

    public bool Equals(PrimaryKey x, PrimaryKey y)
    {
        return x.GetHashCode() == y.GetHashCode() && x.Value.Equals(y.Value);
    }

    public override bool Equals(object obj)
    {
        //Check for null and compare run-time types.
        if ((obj == null) || obj is not PrimaryKey key)
        {
            return false;
        }
        else
        {
            return Equals(this, key);
        }
    }

    public override int GetHashCode()
    {
        return GetHashCode(this);
    }

    public int GetHashCode([DisallowNull] PrimaryKey obj)
    {
        var hashCode = new HashCode();
        hashCode.Add(PropertyName);
        hashCode.Add(ColumnName);
        hashCode.Add(Value);
        hashCode.Add(IsAutoIncrement);   
        return hashCode.ToHashCode();
    }

    public static implicit operator (string propertyName, string ColumnName, object Id)(PrimaryKey value)
    {
        return (value.PropertyName, value.ColumnName, value.Value);
    }

    public static implicit operator PrimaryKey((string propertyName, string ColumnName, object Id, bool isAutoIncrement) value)
    {
        return new PrimaryKey(value.propertyName, value.ColumnName, value.Id, value.isAutoIncrement);
    }
}