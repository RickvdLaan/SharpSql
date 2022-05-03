using SharpSql.Attributes;
using System;
using System.Collections.Generic;

namespace SharpSql;

public sealed class SharpSqlCache
{
    internal static Dictionary<Type, Type> CollectionEntityRelations { get; private set; }

    internal static Dictionary<(Type CollectionTypeLeft, Type CollectionTypeRight), SharpSqlTableAttribute> ManyToManyRelations { get; private set; }

    internal static HashSet<(Type EntityType, string ColumnName)> UniqueConstraints { get; private set; }

    internal static Dictionary<Type, Dictionary<string, ColumnType>> EntityColumns { get; private set; }

    internal static Dictionary<Type, List<string>> MutableColumns { get; private set; }

    internal static Dictionary<Type, SharpSqlPrimaryKey> PrimaryKeys { get; private set; }

    internal static Dictionary<Type, byte> ManyToMany { get; private set; }

    public SharpSqlCache()
    {
        CollectionEntityRelations = new Dictionary<Type, Type>();
        ManyToManyRelations = new Dictionary<(Type CollectionTypeLeft, Type CollectionTypeRight), SharpSqlTableAttribute>();
        UniqueConstraints = new HashSet<(Type EntityType, string ColumnName)>();
        EntityColumns = new Dictionary<Type, Dictionary<string, ColumnType>>();
        MutableColumns = new Dictionary<Type, List<string>>();
        ManyToMany = new Dictionary<Type, byte>();
        PrimaryKeys = new Dictionary<Type, SharpSqlPrimaryKey>();
    }

    public static string GetTableNameFromEntity(SharpSqlEntity entity)
    {
        return CollectionEntityRelations[entity.GetType()].Name;
    }
}