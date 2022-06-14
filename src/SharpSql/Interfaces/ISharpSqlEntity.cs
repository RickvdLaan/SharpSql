using System;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace SharpSql.Interfaces;

public interface ISharpSqlEntity
{
    public string ExecutedQuery { get; }

    public bool DisableChangeTracking { get; }

    public bool IsAutoIncrement { get; }

    public bool IsNew { get; }

    public bool IsDirty { get; }

    public ObjectState ObjectState { get; }

    public SharpSqlPrimaryKey PrimaryKey { get; }

    public ReadOnlyCollection<string> TableScheme { get; }

    void Save();

    void Delete();

    bool IsFieldDirty(string field);

    void MarkFieldsAsDirty(params string[] fields);

    SharpSqlEntity Inner();

    SharpSqlEntity Left();

    SharpSqlEntity FetchUsingUC(string columnName, string value);

    SharpSqlEntity FetchEntityByPrimaryKey(object primaryKey);

    SharpSqlEntity FetchEntityByPrimaryKey(params object[] primaryKeys);

    SharpSqlEntity FetchEntityByPrimaryKey<EntityType>(object primaryKey, Expression<Func<EntityType, object>> joins) where EntityType : SharpSqlEntity;

    SharpSqlEntity FetchEntityByPrimaryKey<EntityType>(Expression<Func<EntityType, object>> joins, params object[] primaryKeys) where EntityType : SharpSqlEntity;
}