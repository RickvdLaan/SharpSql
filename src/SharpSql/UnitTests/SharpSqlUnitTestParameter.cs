using System;

namespace SharpSql.UnitTests;

public sealed class SharpSqlUnitTestParameter
{
    internal string MemoryTableName { get; }

    internal Type EntityType { get; }

    public SharpSqlUnitTestParameter(string memoryTableName, Type entityType)
    {
        MemoryTableName = memoryTableName;
        EntityType = entityType;
    }
}