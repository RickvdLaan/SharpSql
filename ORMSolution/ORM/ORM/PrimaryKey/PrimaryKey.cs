using ORM.Interfaces;
using System.Collections.Generic;

namespace ORM
{
    internal struct PrimaryKey : IORMPrimaryKey
    {
        public string ColumnName { get; set; }

        public object Value { get; set; }

        public PrimaryKey(string columnName, object id)
        {
            ColumnName = columnName;
            Value = id;
        }

        public override string ToString()
        {
            return $"{ColumnName}: {Value}";
        }

        public override bool Equals(object obj)
        {
            return obj is PrimaryKey other &&
                   ColumnName == other.ColumnName &&
                   EqualityComparer<object>.Default.Equals(Value, other.Value);
        }

        public override int GetHashCode()
        {
            return System.HashCode.Combine(ColumnName, Value);
        }

        public void Deconstruct(out string columnName, out object id)
        {
            columnName = ColumnName;
            id = Value;
        }

        public static implicit operator (string ColumnName, object Id)(PrimaryKey value)
        {
            return (value.ColumnName, value.Value);
        }

        public static implicit operator PrimaryKey((string ColumnName, object Id) value)
        {
            return new PrimaryKey(value.ColumnName, value.Id);
        }
    }
}
