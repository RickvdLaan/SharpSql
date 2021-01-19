using ORM;
using ORM.Attributes;

namespace ORMFakeDAL
{
    public class Role : ORMEntity
    {
        [ORMPrimaryKey, ORMColumn(RoleConstants.ColumnId)]
        public int RoleId { get; private set; } = -1;

        [ORMColumn(RoleConstants.ColumnName)]
        public string Description { get; set; }

        public Role() { }

        public Role(int id)
        {
            base.FetchEntityByPrimaryKey(id);
        }
    }
}
