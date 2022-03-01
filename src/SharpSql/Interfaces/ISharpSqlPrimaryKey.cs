namespace SharpSql.Interfaces;

public interface ISharpSqlPrimaryKey
{
    string PropertyName { get; set; }

    string ColumnName { get; set; }

    object Value { get; set; }

    bool IsAutoIncrement { get; set; }
}