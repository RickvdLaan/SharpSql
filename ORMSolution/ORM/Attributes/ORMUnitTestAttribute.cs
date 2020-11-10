using System;

namespace ORM.Attributes
{
    public sealed class ORMUnitTestAttribute : Attribute
    {
        internal string MemoryTableName { get; }

        public ORMUnitTestAttribute() { }

        public ORMUnitTestAttribute(string memoryTableName)
        {
            MemoryTableName = memoryTableName;
        }
    }
}
