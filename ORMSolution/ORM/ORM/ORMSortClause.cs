using ORM.Interfaces;

namespace ORM
{
    public sealed class ORMSortClause : IORMSortClause
    {
        public ORMEntityField Field { get; set; }
        public ORMSortType SortType { get; set; }

        public ORMSortClause(ORMEntityField field, ORMSortType sortType)
        {
            Field = field;
            SortType = sortType;
        }
    }
}
