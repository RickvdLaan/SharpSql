using ORM.Attributes;

namespace ORM
{
    public enum ORMOrderByType
    {
        [ORMSQL("ASC")]
        Ascending,
        [ORMSQL("DESC")]
        Descending
    }
}
