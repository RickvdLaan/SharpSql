using ORM.Interfaces;
using System;
using System.Collections.Generic;

namespace ORM
{
    internal struct PrimaryKey : IORMPrimaryKey
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

        public override bool Equals(object obj)
        {
            return obj is PrimaryKey other &&
                   ColumnName == other.ColumnName &&
                   EqualityComparer<object>.Default.Equals(Value, other.Value);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ColumnName, Value);
        }

        public void Deconstruct(out string columnName, out object id)
        {
            columnName = ColumnName;
            id = Value;
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
}
