using ORM.Attributes;

namespace ORM
{
    public enum ORMSortType
    {
        [ORMSQL("ASC")]
        Ascending,
        [ORMSQL("DESC")]
        Descending
    }
}
