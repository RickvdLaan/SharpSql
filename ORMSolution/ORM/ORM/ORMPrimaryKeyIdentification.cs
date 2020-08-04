using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace ORM.ORM
{
    internal struct ORMPrimaryKeyIdentification
    {
        public int HashCode { get; private set; }
        public object[] PKValues { get; private set; }

        internal ORMPrimaryKeyIdentification(DbDataReader reader, int[] primaryKeyIndexes)
        {
            if (primaryKeyIndexes == null)
            {
                throw new ArgumentException("Primary keys not set");
            }

            PKValues = new object[primaryKeyIndexes.Length];
            var hashCode = new HashCode();
            for (int i = 0; i < primaryKeyIndexes.Length; i++)
            {
                var value = reader.GetValue(i);
                PKValues[i] = value;
                hashCode.Add(value);
            }

            HashCode = hashCode.ToHashCode();
        }


        internal static int[] DeterminePrimaryKeyIndexes(DbDataReader reader, ORMEntity entity)
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

    internal class ORMPrimaryKeyIdentificationComparer : IEqualityComparer<ORMPrimaryKeyIdentification>
    {
        public bool Equals(ORMPrimaryKeyIdentification x, ORMPrimaryKeyIdentification y) => x.HashCode == y.HashCode && Enumerable.SequenceEqual(x.PKValues, y.PKValues);

        public int GetHashCode(ORMPrimaryKeyIdentification obj) => obj.HashCode;
    }
}
