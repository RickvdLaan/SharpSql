using ORM.Interfaces;
using System.Collections.Generic;

namespace ORM
{
    public class ORMCombinedPrimaryKey
    {
        public int Count => Keys.Count;

        public List<IORMPrimaryKey> Keys { get; private set; }

        public ORMCombinedPrimaryKey(int totalAmountOfKeys)
        {
            Keys = new List<IORMPrimaryKey>(totalAmountOfKeys);
        }

        public void Add(string columnName, object value)
        {
            Keys.Add(new ORMPrimaryKey(columnName, value));
        }

        internal struct ORMPrimaryKey : IORMPrimaryKey
        {
            public string ColumnName { get; set; }

            public object Value { get; set; }

            public ORMPrimaryKey(string columnName, object id)
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
                return obj is ORMPrimaryKey other &&
                       ColumnName == other.ColumnName &&
                       EqualityComparer<object>.Default.Equals(Value, other.Value);
            }

            public override int GetHashCode()
            {
                int hashCode = -1375677921;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ColumnName);
                hashCode = hashCode * -1521134295 + EqualityComparer<object>.Default.GetHashCode(Value);
                return hashCode;
            }

            public void Deconstruct(out string columnName, out object id)
            {
                columnName = ColumnName;
                id = Value;
            }

            public static implicit operator (string ColumnName, object Id)(ORMPrimaryKey value)
            {
                return (value.ColumnName, value.Value);
            }

            public static implicit operator ORMPrimaryKey((string ColumnName, object Id) value)
            {
                return new ORMPrimaryKey(value.ColumnName, value.Id);
            }
        }
    }
}