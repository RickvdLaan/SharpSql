using ORM.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace ORM
{
    public class ORMPrimaryKey : IEqualityComparer<ORMPrimaryKey>
    {
        public int HashCode { get; private set; }

        public int Count => Keys.Count;

        public List<IORMPrimaryKey> Keys { get; private set; }

        internal ORMPrimaryKey() { }

        public ORMPrimaryKey(int totalAmountOfKeys)
        {
            Keys = new List<IORMPrimaryKey>(totalAmountOfKeys);
        }

        internal ORMPrimaryKey(IDataReader reader, int[] primaryKeyIndexes)
            : this(primaryKeyIndexes.Length)
        {
            if (primaryKeyIndexes == null)
            {
                throw new ArgumentException("Primary keys not set");
            }

            var hashCode = new HashCode();
            for (int i = 0; i < primaryKeyIndexes.Length; i++)
            {
                Add(string.Empty, reader.GetName(i), reader.GetValue(i));
                hashCode.Add(reader.GetValue(i));
            }

            HashCode = hashCode.ToHashCode();
        }

        public int GetHashCode(ORMPrimaryKey obj) => obj.HashCode;

        public bool Equals(ORMPrimaryKey x, ORMPrimaryKey y) => x.HashCode == y.HashCode && Enumerable.SequenceEqual(x.Keys, y.Keys);

        public void Add(string propertyName, string columnName, object value)
        {
            Keys.Add(new PrimaryKey(propertyName, columnName, value));
        }

        internal static int[] DeterminePrimaryKeyIndexes(IDataReader reader, ORMEntity entity)
        {
            var pks = entity.GetPrimaryKeyPropertyInfo();
            var primaryKeyIndexes = new int[pks.Length];
            int found = 0;

            for (int i = 0; i < reader.FieldCount && found < pks.Length; i++)
            {
                var name = reader.GetName(i);
                if (pks.Any(x => string.Compare(x.Name, name, true) == 0))
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
}