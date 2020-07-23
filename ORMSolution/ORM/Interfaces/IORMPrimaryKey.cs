namespace ORM.Interfaces
{
    public interface IORMPrimaryKey
    {
        string ColumnName { get; set; }

        object Value { get; set; }
    }
}
