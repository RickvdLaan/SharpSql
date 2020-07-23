using ORM.Interfaces;
using System.Collections.Generic;

namespace ORM
{
    public class ORMCombinedPrimaryKey
    {
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

            public ORMPrimaryKey(string field, object id)
            {
                ColumnName = field;
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

            public void Deconstruct(out string field, out object id)
            {
                field = ColumnName;
                id = Value;
            }

            public static implicit operator (string Field, object Id)(ORMPrimaryKey value)
            {
                return (value.ColumnName, value.Value);
            }

            public static implicit operator ORMPrimaryKey((string Field, object Id) value)
            {
                return new ORMPrimaryKey(value.Field, value.Id);
            }
        }
    }
}