namespace SharpSql.Interfaces
{
    public interface IORMPrimaryKey
    {
        string PropertyName { get; set; }

        string ColumnName { get; set; }

        object Value { get; set; }

        bool IsAutoIncrement { get; set; }
    }
}
