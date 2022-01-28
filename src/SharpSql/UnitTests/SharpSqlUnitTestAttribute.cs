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
        MemoryTables.Add(new SharpSqlUnitTestParameter(memoryTable, null));
    }

    public SharpSqlUnitTestAttribute(string memoryTable, Type type)
    {
        MemoryTables.Add(new SharpSqlUnitTestParameter(memoryTable, type));
    }

    public SharpSqlUnitTestAttribute(string memoryTable1, Type type1, string memoryTable2, Type type2)
    {
        MemoryTables.Add(new SharpSqlUnitTestParameter(memoryTable1, type1));
        MemoryTables.Add(new SharpSqlUnitTestParameter(memoryTable2, type2));
    }
}