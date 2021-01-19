using System;
using System.Collections.Generic;

namespace ORM.Attributes
{
    public sealed class ORMUnitTestAttribute : Attribute
    {
        internal List<ORMUnitTestParameter> MemoryTables = new List<ORMUnitTestParameter>();

        public ORMUnitTestAttribute() { }

        public ORMUnitTestAttribute(string memoryTable)
        {
            MemoryTables.Add(new ORMUnitTestParameter(memoryTable, null));
        }

        public ORMUnitTestAttribute(string memoryTable, Type type)
        {
            MemoryTables.Add(new ORMUnitTestParameter(memoryTable, type));
        }

        public ORMUnitTestAttribute(string memoryTable1, Type type1, string memoryTable2, Type type2)
        {
            MemoryTables.Add(new ORMUnitTestParameter(memoryTable1, type1));
            MemoryTables.Add(new ORMUnitTestParameter(memoryTable2, type2));
        }
    }

    public sealed class ORMUnitTestParameter
    {
        internal string MemoryTableName { get; }

        internal Type EntityType { get; }

        public ORMUnitTestParameter(string memoryTableName, Type entityType)
        {
            MemoryTableName = memoryTableName;
            EntityType = entityType;
        }
    }
}
