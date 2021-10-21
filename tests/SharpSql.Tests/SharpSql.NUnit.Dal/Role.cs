using SharpSql.Attributes;

namespace SharpSql.NUnit
{
    public class Role : SharpSqlEntity
    {
        [SharpSqlPrimaryKey, SharpSqlColumn(RoleConstants.ColumnId)]
        public int RoleId { get; private set; } = -1;

        [SharpSqlColumn(RoleConstants.ColumnName)]
        public string Description { get; set; }

        public Role() { }

        public Role(int id)
        {
            base.FetchEntityByPrimaryKey(id);
        }
    }
}
