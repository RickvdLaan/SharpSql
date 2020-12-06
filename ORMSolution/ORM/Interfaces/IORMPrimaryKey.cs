namespace ORM.Interfaces
{
    public interface IORMPrimaryKey
    {
        string PropertyName { get; set; }

        string ColumnName { get; set; }

        object Value { get; set; }
    }
}
