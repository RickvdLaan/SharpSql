using System;
using System.Collections.Generic;

namespace SharpSql.UnitTests;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class SharpSqlUnitTestAttribute : Attribute
{
    internal List<SharpSqlUnitTestParameter> MemoryTables = new();

    public SharpSqlUnitTestAttribute() { }

    public SharpSqlUnitTestAttribute(string memoryTable)
    {
        MemoryTables.Add(new SharpSqlUnitTestParameter(memoryTable));
    }

    internal SharpSqlUnitTestAttribute(string memoryTable, ColumnType columnType)
    {
        MemoryTables.Add(new SharpSqlUnitTestParameter(memoryTable, columnType));
    }
}