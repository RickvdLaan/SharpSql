namespace ORM.Interfaces
{
    public interface IORMSortClause
    {
        ORMEntityField Field { get; set; }
        ORMSortType SortType { get; set; }
    }
}
