using System;

namespace SharpSql.UnitTests;

public sealed class SharpSqlUnitTestParameter
{
    internal string MemoryTableName { get; init; }

    internal ColumnType ColumnType { get; init; }

    public SharpSqlUnitTestParameter(string memoryTableName)
    {
        MemoryTableName = memoryTableName;
    }

    internal SharpSqlUnitTestParameter(string memoryTableName, ColumnType columnType)
    {
        MemoryTableName = memoryTableName;
        ColumnType = columnType;
    }
}